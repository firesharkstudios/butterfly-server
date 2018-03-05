CREATE TABLE notify_message (
	id VARCHAR(50) NOT NULL,
	message_type TINYINT NOT NULL,
	message_priority TINYINT NOT NULL,
	message_from VARCHAR(255) NOT NULL,
	message_to VARCHAR(1024) NOT NULL,
	message_subject VARCHAR(255) NULL,
	message_body_text MEDIUMTEXT NULL,
	message_body_html MEDIUMTEXT NULL,
	sent_at DATETIME NULL,
	send_error VARCHAR(255) NULL,
	created_at DATETIME NOT NULL,
	PRIMARY KEY (id),
	INDEX other (message_type, sent_at, message_priority, created_at)
);

