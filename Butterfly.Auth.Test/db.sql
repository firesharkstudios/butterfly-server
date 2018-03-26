CREATE TABLE account (
	id VARCHAR(44) NOT NULL,
	created_at INT NOT NULL,
	updated_at INT NOT NULL,
	PRIMARY KEY(id)
);

CREATE TABLE user (
	id VARCHAR(44) NOT NULL,
	account_id VARCHAR(44) NOT NULL,
	username VARCHAR(40) NOT NULL,
	first_name VARCHAR(255) NOT NULL,
	last_name VARCHAR(255) NOT NULL,
	email VARCHAR(255) NOT NULL,
	email_verified_at INT NULL,
	phone VARCHAR(20) NULL,
	phone_verified_at INT NULL
	salt VARCHAR(40) NOT NULL,
	password_hash VARCHAR(90) NOT NULL,	
	reset_code VARCHAR(6) NULL,	
	reset_code_expires_at INT NULL,	
	created_at INT NOT NULL,
	updated_at INT NOT NULL,
	PRIMARY KEY(id),
	UNIQUE INDEX username (username)
);

CREATE TABLE auth_token (
	id VARCHAR(44) NOT NULL,
	user_id VARCHAR(44) NOT NULL,
	expires_at INT NOT NULL,
	created_at INT NOT NULL,
	PRIMARY KEY(id)
);
