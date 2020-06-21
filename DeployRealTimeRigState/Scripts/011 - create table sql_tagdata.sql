CREATE TABLE if not exists `sql_tagdata` (
  `tagpath` varchar(8192) NOT NULL,
  `intvalue` bigint(20) DEFAULT NULL,
  `floatvalue` double DEFAULT NULL,
  `stringvalue` varchar(8192) DEFAULT NULL,
  `datevalue` datetime DEFAULT NULL,
  `dataintegrity` int(11) DEFAULT NULL,
  `t_stamp` bigint(20) NOT NULL,
  KEY `IDX_SQL_TagData` (`tagpath`(767),`t_stamp`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
