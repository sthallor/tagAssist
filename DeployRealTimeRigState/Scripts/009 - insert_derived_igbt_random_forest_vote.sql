drop procedure if exists `insert_derived_igbt_random_forest_vote`;
DELIMITER $$
CREATE DEFINER=`root`@`localhost` PROCEDURE `insert_derived_igbt_random_forest_vote`(IN `timestamp_value` BIGINT, IN `igbt_rfvote_value` FLOAT)
    MODIFIES SQL DATA
BEGIN
   select concat('insert into sql_tagdata select ''edge_analytics/igbt_rfvote'',null,',igbt_rfvote_value,',null,null,192,',timestamp_value) into @insert_sql;
   prepare stmt from @insert_sql; execute stmt; deallocate prepare stmt;
   select 1 as return_code;
END$$
DELIMITER ;
