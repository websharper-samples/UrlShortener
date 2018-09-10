module UrlShortener.Database

open System
open FSharp.Data.Sql
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open WebSharper
open WebSharper.AspNetCore

type Sql = SqlDataProvider<
            // Connect to SQLite using System.Data.Sqlite.
            Common.DatabaseProviderTypes.SQLITE,
            SQLiteLibrary = Common.SQLiteLibrary.SystemDataSQLite,
            ResolutionPath = const(__SOURCE_DIRECTORY__ + "/../packages/System.Data.SQLite.Core/lib/netstandard2.0/"),
            // Store the database file in db/urlshortener.db.
            ConnectionString = const("Data Source=" + __SOURCE_DIRECTORY__ + "/db/urlshortener.db"),
            // Store the schema as JSON so that the compiler doesn't need the database to exist.
            ContextSchemaPath = const(__SOURCE_DIRECTORY__ + "/db/urlshortener.schema.json"),
            UseOptionTypes = true>

/// ASP.NET Core service that creates a data context per request.
type Context(config: IConfiguration, logger: ILogger<Context>) =
    do logger.LogInformation("Creating db context")

    let db =
        config.GetSection("ConnectionStrings").["UrlShortener"]
        |> Sql.GetDataContext

    /// Apply all migrations.
    member this.Migrate() =
        try
            use ctx = db.CreateConnection()
            let evolve =
                new Evolve.Evolve(ctx, logger.LogInformation,
                    Locations = ["db/migrations"],
                    IsEraseDisabled = true)
            evolve.Migrate()
        with ex ->
            logger.LogCritical("Database migration failed: {0}", ex)

    /// Get the user for this Facebook user id, or create a user if there isn't one.
    member this.GetOrCreateFacebookUser(fbUserId: string, fbUserName: string) = async {
        let existing =
            query { for u in db.Main.User do
                    where (u.FacebookId = fbUserId)
                    select (Some u.Id)
                    headOrDefault }
        match existing with
        | None ->
            let u =
                db.Main.User.Create(
                    Id = Guid.NewGuid(),
                    FacebookId = fbUserId,
                    FullName = fbUserName)
            do! db.SubmitUpdatesAsync()
            return u.Id
        | Some id ->
            return id
    }

    /// Get the user's full name.
    member this.GetFullName(userId: Guid) = async {
        let u =
            query { for u in db.Main.User do
                    where (u.Id = userId)
                    select (Some u.FullName)
                    headOrDefault }
        return u
    }

    /// Create a new link on this user's behalf, pointing to this url.
    /// Returns the slug for this new link.
    member this.CreateLink(userId: Guid, url: string) = async {
        let r =
            db.Main.Redirection.Create(
                Id = int64 (Random().Next()),
                CreatorId = userId,
                Url = url)
        do! db.SubmitUpdatesAsync()
        return string r.Id
    }

    /// Get the url pointed to by the given slug, if any.
    member this.TryGetLink(slug: string) = async {
        match Int64.TryParse slug with
        | true, linkId ->
            let u =
                query { for u in db.Main.Redirection do
                        where (u.Id = linkId)
                        select (Some u.Url)
                        headOrDefault }
            return u
        | false, _ ->
            return None
    }

type Web.Context with
    /// Get the database context for the current request.
    member this.Db =
        this.HttpContext().RequestServices.GetRequiredService<Context>()
