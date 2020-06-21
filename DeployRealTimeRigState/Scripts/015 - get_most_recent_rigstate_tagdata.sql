﻿drop procedure if exists `get_most_recent_rigstate_tagdata`;
DELIMITER $$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_most_recent_rigstate_tagdata`()
LANGUAGE SQL
NOT DETERMINISTIC
CONTAINS SQL
SQL SECURITY DEFINER
COMMENT ''
BEGIN
declare last_tstamp bigint;
select max(t_stamp) into last_tstamp from sql_tagdata where tagpath='edge_analytics/rig_state';
if (last_tstamp>((UNIX_TIMESTAMP()*1000)-310000)) then /* only return a value as long as it was derived within the past 5 minutes */
select d.floatvalue,d.t_stamp from sql_tagdata d where d.t_stamp=last_tstamp and d.tagpath='edge_analytics/rig_state';
end if;
END$$
DELIMITER ;
