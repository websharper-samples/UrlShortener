module UrlShortener.Database

open System
open FSharp.Data.Sql
open Microsoft.Data.Sqlite
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

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

let private getConnectionString (config: IConfiguration) =
    config.GetSection("ConnectionStrings").["UrlShortener"]

/// Apply all migrations.
let Migrate (config: IConfiguration) (logger: ILogger) =
    try
        use ctx = new SqliteConnection(getConnectionString config)
        let evolve =
            new Evolve.Evolve(ctx, logger.LogInformation,
                Locations = ["db/migrations"],
                IsEraseDisabled = true)
        evolve.Migrate()
    with ex ->
        logger.LogCritical("Database migration failed: {0}", ex)

/// Get a SQL data context.
let GetDataContext (config: IConfiguration) =
    Sql.GetDataContext(getConnectionString config)

/// Get the user for this Facebook user id, or create a user if there isn't one.
let GetOrCreateFacebookUser (db: Sql.dataContext) (fbUserId: string) (fbUserName: string) = async {
    let existing =
        query { for u in db.Main.User do
                where (u.FacebookId = fbUserId)
                select (Some u.Id)
                headOrDefault }
    match existing with
    | None ->
        let id = Guid.NewGuid()
        let _u =
            db.Main.User.Create(
                Id = id,
                FacebookId = fbUserId,
                FullName = fbUserName)
        do! db.SubmitUpdatesAsync()
        return id
    | Some id ->
        return id
}

/// Get the user's full name.
let GetFullName (db: Sql.dataContext) (userId: Guid) = async {
    let u =
        query { for u in db.Main.User do
                where (u.Id = userId)
                select (Some u.FullName)
                headOrDefault }
    return u
}