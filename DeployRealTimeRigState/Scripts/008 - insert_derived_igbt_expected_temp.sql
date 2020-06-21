drop procedure if exists `insert_derived_igbt_expected_temp`;
DELIMITER $$
CREATE DEFINER=`root`@`localhost` PROCEDURE `insert_derived_igbt_expected_temp`(IN `timestamp_value` BIGINT, IN `igbt_expected_temp_value` FLOAT)
    MODIFIES SQL DATA
BEGIN
   select concat('insert into sql_tagdata select ''edge_analytics/max_expected_igbt_temp'',null,',igbt_expected_temp_value,',null,null,192,',timestamp_value) into @insert_sql;
   prepare stmt from @insert_sql; execute stmt; deallocate prepare stmt;
   select 1 as return_code;
END$$
DELIMITER ;
