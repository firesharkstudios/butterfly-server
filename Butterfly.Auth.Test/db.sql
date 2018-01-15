CREATE TABLE account (
	id VARCHAR(44) NOT NULL,
	created_at DATETIME NOT NULL,
	updated_at DATETIME NOT NULL,
	PRIMARY KEY(id)
);

CREATE TABLE user (
	id VARCHAR(44) NOT NULL,
	account_id VARCHAR(44) NOT NULL,
	username VARCHAR(40) NOT NULL,
	first_name VARCHAR(255) NOT NULL,
	last_name VARCHAR(255) NOT NULL,
	email VARCHAR(255) NOT NULL,
	salt VARCHAR(40) NOT NULL,
	password_hash VARCHAR(90) NOT NULL,	
	created_at DATETIME NOT NULL,
	updated_at DATETIME NOT NULL,
	PRIMARY KEY(id),
	UNIQUE INDEX username (username)
);

CREATE TABLE auth_token (
	id VARCHAR(44) NOT NULL,
	user_id VARCHAR(44) NOT NULL,
	expires_at DATETIME NOT NULL,
	created_at DATETIME NOT NULL,
	PRIMARY KEY(id)
);
