drop procedure if exists `get_most_recent_avg_toolface`;
DELIMITER $$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_most_recent_avg_toolface`()
BEGIN
   declare last_tstamp bigint;
   select max(t_stamp) into last_tstamp from sql_tagdata where tagpath='edge_analytics/avg_toolface';
   select d.floatvalue,d.t_stamp from sql_tagdata d where d.t_stamp=last_tstamp and d.tagpath='edge_analytics/avg_toolface';
END$$
DELIMITER ;


effectiveness