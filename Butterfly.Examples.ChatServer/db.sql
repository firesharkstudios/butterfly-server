DROP TABLE IF EXISTS user;
CREATE TABLE user (
	id VARCHAR(40) NOT NULL,
	name VARCHAR(40) NOT NULL,
	created_at DATETIME NOT NULL,
	updated_at DATETIME NOT NULL,
	PRIMARY KEY(id)
);

DROP TABLE IF EXISTS chat;
CREATE TABLE chat (
	id VARCHAR(40) NOT NULL,
	name VARCHAR(40) NOT NULL,
	join_id VARCHAR(8) NOT NULL,
	owner_id VARCHAR(40) NOT NULL,
	created_at DATETIME NOT NULL,
	PRIMARY KEY(id)
);
CREATE UNIQUE INDEX joinId ON chat (join_id);

DROP TABLE IF EXISTS chat_participant;
CREATE TABLE chat_participant (
	id VARCHAR(40) NOT NULL,
	chat_id VARCHAR(40) NOT NULL,
	user_id VARCHAR(40) NOT NULL,
	created_at DATETIME NOT NULL,
	PRIMARY KEY(id)
);
CREATE UNIQUE INDEX chatIdUserId ON chat_participant (chat_id, user_id);

DROP TABLE IF EXISTS chat_message;
CREATE TABLE chat_message (
	id VARCHAR(40) NOT NULL,
	chat_id VARCHAR(40) NOT NULL,
	user_id VARCHAR(40) NOT NULL,
	text VARCHAR(255) NOT NULL,
	created_at DATETIME NOT NULL,
	PRIMARY KEY(id)
);
