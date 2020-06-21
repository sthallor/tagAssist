drop procedure if exists `get_last_20minutes_igbt_data`;
DELIMITER $$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_last_20minutes_igbt_data`()
BEGIN
   declare howManySecondsToLiveInPast int default 20;
   declare 20minutesAgo,rightNow,bigintCounter bigint default 0;
   declare i int default 0;
   declare IGBT_TemperatureTagID,DW_OutputTagID,DC_Bus_TagID,DrawworksSpeedTagID,HookloadTagID,BlockHeightTagID,TorqueTagID,DW_CurrentTagID,DW_FrequencyTagID int;
   declare IGBT_TemperatureUOM,DW_OutputUOM,DC_BusUOM,DrawworksSpeedUOM,HookloadUOM,BlockHeightUOM,TorqueUOM,DW_CurrentUOM,DW_FrequencyUOM varchar(50);
   declare Partition1,Partition2 varchar(40);
   declare previous_IGBTtemp,previous_dwOutput,previous_dcBus,previous_Hookload,previous_dwSpeed,previous_dwCurrent,previous_dwFrequency,previous_Torque,previous_BlockHeight float;

   drop temporary table if exists temp_sqlth_data_1;
   drop temporary table if exists temp_sqlth_data_2;
   drop temporary table if exists temp_sqlth_data_3;
   drop temporary table if exists temp_sqlth_data_4;
   drop temporary table if exists temp_sqlth_data_5;
   drop temporary table if exists temp_sqlth_data_6;
   drop temporary table if exists temp_sqlth_data_7;
   drop temporary table if exists temp_sqlth_data_8;
   drop temporary table if exists temp_sqlth_data_9;
	drop temporary table if exists `base_1`;
   drop temporary table if exists `base_2`;
   drop temporary table if exists `base_3`;
   drop temporary table if exists `base_4`;
   drop temporary table if exists `base_5`;
   drop temporary table if exists `base_6`;
   drop temporary table if exists `base_7`;
   drop temporary table if exists `base_8`;
   drop temporary table if exists `base_9`;
   
   set 20minutesAgo = round((unix_timestamp()-1200)*1000,-4)-10000-(howManySecondsToLiveInPast*1000);  /* Now minus 20 minutes, multiply by 1000 for milliseconds; minus 10 seconds to get 20 minutes + 10 seconds */
   set rightNow = 20minutesAgo + 1210000; /* don't add howManySecondsToLiveInPast, we want to live 'x' seconds in the past */
   select pname into Partition1 from sqlth_partitions where 20minutesAgo>=start_time and 20minutesAgo<end_time;
   select concat('create temporary table temp_sqlth_data_1 select t_stamp,tagid,floatvalue,intvalue from ',Partition1,' where t_stamp>=',20minutesAgo) into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select pname into Partition2 from sqlth_partitions where (rightNow>=start_time and rightNow<end_time) and (20minutesAgo<start_time);
   if (isnull(Partition2)=0) then
      select concat('insert into temp_sqlth_data_1 select t_stamp,tagid,floatvalue,intvalue from ',Partition2,' where t_stamp>=',20minutesAgo) into @s;
      prepare stmt from @s; execute stmt; deallocate prepare stmt;
   end if;

   set i = 0;
   drop temporary table if exists 20minutes;
   create temporary table 20minutes(tenseconds_epoch bigint);
   while i <= 120 do
      insert into 20minutes values(20minutesAgo+(i*10000));
      set i = i + 1;
   end while;

   select te.id,tags.units into IGBT_TemperatureTagID,IGBT_TemperatureUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='IGBT Temperature' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into DW_OutputTagID,DW_OutputUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='DW Output Voltage' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into DC_Bus_TagID,DC_BusUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='DW DC Bus Voltage' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into HookloadTagID,HookloadUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Hookload' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into BlockHeightTagID,BlockHeightUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Block Height' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into TorqueTagID,TorqueUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Drawworks Torque' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into DrawworksSpeedTagID,DrawworksSpeedUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='DW Speed' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into DW_CurrentTagID,DW_CurrentUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='DW Current' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into DW_FrequencyTagID,DW_FrequencyUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='DW Frequency' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);

-- due to a MySQL limitation, have to copy the temp_sqlth_data1 table several times as a temporary table is only allowed to referenced once in a query
   create temporary table temp_sqlth_data_2 select * from temp_sqlth_data_1 where tagid=DW_OutputTagID;
   create temporary table base_2 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_2 group by round(t_stamp,-4);
   create temporary table temp_sqlth_data_3 select * from temp_sqlth_data_1 where tagid=DC_Bus_TagID;
   create temporary table base_3 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_3 group by round(t_stamp,-4);
   create temporary table temp_sqlth_data_4 select * from temp_sqlth_data_1 where tagid=HookloadTagID;
   create temporary table base_4 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_4 group by round(t_stamp,-4);
   create temporary table temp_sqlth_data_5 select * from temp_sqlth_data_1 where tagid=BlockHeightTagID;
   create temporary table base_5 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_5 group by round(t_stamp,-4);
   create temporary table temp_sqlth_data_6 select * from temp_sqlth_data_1 where tagid=TorqueTagID;
   create temporary table base_6 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_6 group by round(t_stamp,-4);
   create temporary table temp_sqlth_data_7 select * from temp_sqlth_data_1 where tagid=DrawworksSpeedTagID;
	create temporary table base_7 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_7 group by round(t_stamp,-4);
	create temporary table temp_sqlth_data_8 select * from temp_sqlth_data_1 where tagid=DW_CurrentTagID;
	create temporary table base_8 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_8 group by round(t_stamp,-4);
	create temporary table temp_sqlth_data_9 select * from temp_sqlth_data_1 where tagid=DW_FrequencyTagID;
	create temporary table base_9 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_9 group by round(t_stamp,-4);
   delete from temp_sqlth_data_1 where tagid!=IGBT_TemperatureTagID;
   create temporary table base_1 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_1 group by round(t_stamp,-4);

   drop temporary table if exists temp_results;
   create temporary table temp_results
   select 20minutes.tenseconds_epoch,IGBT_temp.tagvalue as IGBT_temp,dw_output.tagvalue as dw_output,DC_Bus.tagvalue as DC_Bus,hookload.tagvalue as Hookload
         ,BlockHeight.tagvalue as BlockHeight,Torque.tagvalue as Torque,dw_speed.tagvalue as dw_speed,dw_current.tagvalue as dw_current,dw_frequency.tagvalue as dw_frequency
         ,IGBT_TemperatureUOM,DW_OutputUOM,DC_BusUOM,HookloadUOM,BlockHeightUOM,TorqueUOM,DrawworksSpeedUOM,DW_CurrentUOM,DW_FrequencyUOM
	  from 20minutes
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_1 base inner join temp_sqlth_data_1 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=20minutesAgo) IGBT_temp on 20minutes.tenseconds_epoch=IGBT_temp.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_2 base inner join temp_sqlth_data_2 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=20minutesAgo) dw_output on 20minutes.tenseconds_epoch=dw_output.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_3 base inner join temp_sqlth_data_3 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=20minutesAgo) DC_Bus on 20minutes.tenseconds_epoch=DC_Bus.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_4 base inner join temp_sqlth_data_4 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=20minutesAgo) hookload on 20minutes.tenseconds_epoch=hookload.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_5 base inner join temp_sqlth_data_5 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=20minutesAgo) BlockHeight on 20minutes.tenseconds_epoch=BlockHeight.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_6 base inner join temp_sqlth_data_6 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=20minutesAgo) Torque on 20minutes.tenseconds_epoch=Torque.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_7 base inner join temp_sqlth_data_7 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=20minutesAgo) dw_speed on 20minutes.tenseconds_epoch=dw_speed.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_8 base inner join temp_sqlth_data_8 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=20minutesAgo) dw_current on 20minutes.tenseconds_epoch=dw_current.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_9 base inner join temp_sqlth_data_9 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=20minutesAgo) dw_frequency on 20minutes.tenseconds_epoch=dw_frequency.nearest10seconds_epoch
   ;
   
   select concat('update temp_results set IGBT_temp=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',IGBT_TemperatureTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',IGBT_TemperatureTagID,' and t_stamp<=',20minutesAgo,')) where tenseconds_epoch=',20minutesAgo,' and IGBT_temp is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set dw_output=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',DW_OutputTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',DW_OutputTagID,' and t_stamp<=',20minutesAgo,')) where tenseconds_epoch=',20minutesAgo,' and dw_output is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set DC_Bus=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',DC_Bus_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',DC_Bus_TagID,' and t_stamp<=',20minutesAgo,')) where tenseconds_epoch=',20minutesAgo,' and DC_Bus is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set Hookload=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',HookloadTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',HookloadTagID,' and t_stamp<=',20minutesAgo,')) where tenseconds_epoch=',20minutesAgo,' and Hookload is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set BlockHeight=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',BlockHeightTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',BlockHeightTagID,' and t_stamp<=',20minutesAgo,')) where tenseconds_epoch=',20minutesAgo,' and BlockHeight is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set Torque=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',TorqueTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',TorqueTagID,' and t_stamp<=',20minutesAgo,')) where tenseconds_epoch=',20minutesAgo,' and Torque is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set dw_speed=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',DrawworksSpeedTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',DrawworksSpeedTagID,' and t_stamp<=',20minutesAgo,')) where tenseconds_epoch=',20minutesAgo,' and dw_speed is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set dw_current=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',DW_CurrentTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',DW_CurrentTagID,' and t_stamp<=',20minutesAgo,')) where tenseconds_epoch=',20minutesAgo,' and dw_current is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set dw_frequency=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',DW_FrequencyTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',DW_FrequencyTagID,' and t_stamp<=',20minutesAgo,')) where tenseconds_epoch=',20minutesAgo,' and dw_frequency is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
     
   set bigintCounter = 20minutesAgo;
   while bigintCounter<=(20minutesAgo + 1210000) do
      update temp_results set IGBT_temp=previous_IGBTtemp where tenseconds_epoch=bigintCounter and IGBT_temp is null;
      update temp_results set dw_output=previous_dwOutput where tenseconds_epoch=bigintCounter and dw_output is null;
      update temp_results set DC_Bus=previous_dcBus where tenseconds_epoch=bigintCounter and DC_Bus is null;
      update temp_results set Hookload=previous_Hookload where tenseconds_epoch=bigintCounter and Hookload is null;
      update temp_results set BlockHeight=previous_BlockHeight where tenseconds_epoch=bigintCounter and BlockHeight is null;
      update temp_results set Torque=previous_Torque where tenseconds_epoch=bigintCounter and Torque is null;
      update temp_results set dw_speed=previous_dwSpeed where tenseconds_epoch=bigintCounter and dw_speed is null;
      update temp_results set dw_current=previous_dwCurrent where tenseconds_epoch=bigintCounter and dw_current is null;
      update temp_results set dw_frequency=previous_dwFrequency where tenseconds_epoch=bigintCounter and dw_frequency is null;
		      
      select IGBT_temp into previous_IGBTtemp from temp_results where tenseconds_epoch=bigintCounter;
      select dw_output into previous_dwOutput from temp_results where tenseconds_epoch=bigintCounter;
      select DC_Bus into previous_dcBus from temp_results where tenseconds_epoch=bigintCounter;
      select Hookload into previous_Hookload from temp_results where tenseconds_epoch=bigintCounter;
      select BlockHeight into previous_BlockHeight from temp_results where tenseconds_epoch=bigintCounter;
      select Torque into previous_Torque from temp_results where tenseconds_epoch=bigintCounter;
      select dw_speed into previous_dwSpeed from temp_results where tenseconds_epoch=bigintCounter;
      select dw_current into previous_dwCurrent from temp_results where tenseconds_epoch=bigintCounter;
      select dw_frequency into previous_dwFrequency from temp_results where tenseconds_epoch=bigintCounter;

      set bigintCounter = bigintCounter + 10000;

   end while;

   select * from temp_results order by tenseconds_epoch;

   drop temporary table if exists temp_sqlth_data_1;
   drop temporary table if exists temp_sqlth_data_2;
   drop temporary table if exists temp_sqlth_data_3;
   drop temporary table if exists temp_sqlth_data_4;
   drop temporary table if exists temp_sqlth_data_5;
   drop temporary table if exists temp_sqlth_data_6;
   drop temporary table if exists temp_sqlth_data_7;
   drop temporary table if exists temp_sqlth_data_8;
   drop temporary table if exists temp_sqlth_data_9;
	drop temporary table if exists `base_1`;
   drop temporary table if exists `base_2`;
   drop temporary table if exists `base_3`;
   drop temporary table if exists `base_4`;
   drop temporary table if exists `base_5`;
   drop temporary table if exists `base_6`;
   drop temporary table if exists `base_7`;
   drop temporary table if exists `base_8`;
   drop temporary table if exists `base_9`;

END$$
DELIMITER ;
