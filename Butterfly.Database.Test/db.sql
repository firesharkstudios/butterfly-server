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

