CREATE DEFINER=`root`@`localhost` PROCEDURE `insert_Sam_records`(IN `t_stamp` BIGINT, IN `tag` VARCHAR(50), IN `tagValue` FLOAT)
	LANGUAGE SQL
	NOT DETERMINISTIC
	CONTAINS SQL
	SQL SECURITY DEFINER
	COMMENT ''
BEGIN
   select concat('insert into sql_tagdata select ''edge_analytics/',tag,''',null,',tagValue,',null,null,192,',t_stamp) into @insert_sql;
   prepare stmt from @insert_sql; execute stmt; deallocate prepare stmt;
   select 1 as return_code;
END