ALTER TABLE `redirection`
	ADD COLUMN `creatorId` GUID NOT NULL REFERENCES `user` (`id`) DEFAULT 0;