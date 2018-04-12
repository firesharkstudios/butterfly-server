CREATE TABLE user (
	id VARCHAR(40) NOT NULL,
	name VARCHAR(40) NOT NULL,
	created_at DATETIME NOT NULL,
	updated_at DATETIME NOT NULL,
	PRIMARY KEY(id)
);

CREATE TABLE chat (
	id VARCHAR(40) NOT NULL,
	name VARCHAR(40) NOT NULL,
	join_id VARCHAR(8) NOT NULL,
	owner_id VARCHAR(40) NOT NULL,
	created_at DATETIME NOT NULL,
	PRIMARY KEY(id),
	UNIQUE INDEX joinId (join_id)
);

CREATE TABLE chat_participant (
	id VARCHAR(40) NOT NULL,
	chat_id VARCHAR(40) NOT NULL,
	user_id VARCHAR(40) NOT NULL,
	created_at DATETIME NOT NULL,
	PRIMARY KEY(id),
	UNIQUE INDEX chatIdUserId (chat_id, user_id)
);

CREATE TABLE chat_message (
	id VARCHAR(40) NOT NULL,
	chat_id VARCHAR(40) NOT NULL,
	user_id VARCHAR(40) NOT NULL,
	text VARCHAR(255) NOT NULL,
	created_at DATETIME NOT NULL,
	PRIMARY KEY(id)
);

-- TRUNCATE chat; TRUNCATE chat_message; TRUNCATE chat_participant; TRUNCATE user;