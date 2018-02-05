-- Uses an auto increment primary key
CREATE TABLE department (
	id int NOT NULL AUTO_INCREMENT,
	name VARCHAR(40) NOT NULL,
	created_at DATETIME NOT NULL,
	updated_at DATETIME NOT NULL,
	PRIMARY KEY(id)
);

-- Uses a single field generated primary key
CREATE TABLE employee (
	id VARCHAR(40) NOT NULL,
	name VARCHAR(40) NOT NULL,
	department_id INT NOT NULL,
	birthday DATETIME NULL,
	created_at DATETIME NOT NULL,
	updated_at DATETIME NOT NULL,
	PRIMARY KEY(id),
	UNIQUE INDEX name (name)
);

-- Uses a multiple field primary key
CREATE TABLE employee_contact (
	employee_id VARCHAR(40) NOT NULL,
	contact_type VARCHAR(10) NOT NULL,
	contact_data VARCHAR(40) NOT NULL,
	created_at DATETIME NOT NULL,
	updated_at DATETIME NOT NULL,
	PRIMARY KEY(employee_id, contact_type)
);

CREATE TABLE estimate_input_option (
	id VARCHAR(45) NOT NULL,
	estimate_input_id VARCHAR(45) NOT NULL,
	seq INT NOT NULL,
	media_file_name VARCHAR(60) NULL,
	name VARCHAR(255) NOT NULL,
	comment VARCHAR(255) NULL,
	created_at DATETIME NOT NULL,
	updated_at DATETIME NOT NULL,
	PRIMARY KEY(id),
	UNIQUE INDEX estimate_input_id_seq (estimate_input_id, seq),
	UNIQUE INDEX estimate_input_id_name (estimate_input_id, name)
);

CREATE TABLE estimate_input_option_var (
	id VARCHAR(45) NOT NULL,
	estimate_input_option_id VARCHAR(45) NOT NULL,
	estimate_input_var_id VARCHAR(45) NOT NULL,
	value FLOAT NOT NULL,
	created_at DATETIME NOT NULL,
	updated_at DATETIME NOT NULL,
	PRIMARY KEY(id),
	UNIQUE INDEX estimate_input_option_id_estimate_input_var_id (estimate_input_option_id, estimate_input_var_id)
);
