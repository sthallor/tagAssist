drop procedure if exists `get_most_recent_effectiveFeedAtAvgTF`;
DELIMITER $$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_most_recent_effectiveFeedAtAvgTF`()
BEGIN
   declare last_tstamp bigint;
   select max(t_stamp) into last_tstamp from sql_tagdata where tagpath='edge_analytics/effectiveFeedAtAvgTF';
   select d.floatvalue,d.t_stamp from sql_tagdata d where d.t_stamp=last_tstamp and d.tagpath='edge_analytics/effectiveFeedAtAvgTF';
END$$
DELIMITER ;