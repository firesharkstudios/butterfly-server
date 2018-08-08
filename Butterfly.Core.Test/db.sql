CREATE TABLE account (
	id VARCHAR(50) NOT NULL,
	created_at INT NOT NULL,
	updated_at INT NOT NULL,
	PRIMARY KEY(id)
);

CREATE TABLE auth_token (
	id VARCHAR(50) NOT NULL,
	user_id VARCHAR(50) NOT NULL,
	expires_at INT NOT NULL,
	created_at INT NOT NULL,
	PRIMARY KEY(id)
);

CREATE TABLE department (
  id INT NOT NULL AUTO_INCREMENT,
  name VARCHAR(40) NOT NULL,
  created_at datetime NOT NULL,
  updated_at datetime NOT NULL,
  PRIMARY KEY (id)
);

CREATE TABLE employee (
  id VARCHAR(40) NOT NULL,
  name VARCHAR(40) NOT NULL,
  department_id INT NOT NULL,
  birthday datetime,
  created_at datetime NOT NULL,
  updated_at datetime NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY name (name)
);

CREATE TABLE employee_contact (
  employee_id VARCHAR(40) NOT NULL,
  contact_type VARCHAR(10) NOT NULL,
  contact_data VARCHAR(40) NOT NULL,
  created_at datetime NOT NULL,
  updated_at datetime NOT NULL,
  PRIMARY KEY (employee_id,contact_type)
);

CREATE TABLE user (
	id VARCHAR(50) NOT NULL,
	account_id VARCHAR(50) NOT NULL,
	username VARCHAR(40) NOT NULL,
	first_name VARCHAR(255) NOT NULL,
	last_name VARCHAR(255) NOT NULL,
	email VARCHAR(255) NOT NULL,
	email_verified_at INT NULL,
	phone VARCHAR(20) NULL,
	phone_verified_at INT NULL,
	role VARCHAR(25) NULL,
	salt VARCHAR(40) NOT NULL,
	password_hash VARCHAR(90) NOT NULL,	
	reset_code VARCHAR(6) NULL,	
	reset_code_expires_at INT NULL,	
	created_at INT NOT NULL,
	updated_at INT NOT NULL,
	PRIMARY KEY(id),
	UNIQUE INDEX username (username)
);