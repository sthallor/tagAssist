drop procedure if exists `insert_derived_rig_state_record`;
DELIMITER $$
CREATE DEFINER=`root`@`localhost` PROCEDURE `insert_derived_rig_state_record`(IN `timestamp_value` BIGINT, IN `rig_state_value` FLOAT)
    MODIFIES SQL DATA
BEGIN
--   declare dataPartition varchar(40);
--   select pname into dataPartition from sqlth_partitions where timestamp_value>=start_time and timestamp_value<end_time;
--   select concat('insert into ',dataPartition,' select id,null,',rig_state_value,',null,null,192,',timestamp_value,' from sqlth_te where tagpath=''edge_analytics/rig_state'' and retired is null') into @insert_sql;
   select concat('insert into sql_tagdata select ''edge_analytics/rig_state'',null,',rig_state_value,',null,null,192,',timestamp_value) into @insert_sql;
   prepare stmt from @insert_sql; execute stmt; deallocate prepare stmt;
   select 1 as return_code;
END$$
DELIMITER ;
