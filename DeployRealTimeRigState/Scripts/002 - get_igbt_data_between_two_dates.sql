drop procedure if exists `get_igbt_data_between_two_dates`;
DELIMITER $$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_igbt_data_between_two_dates`(IN `start_tstamp` BIGINT, IN `stop_tstamp` BIGINT)
BEGIN
   declare bigintCounter bigint default 0;
   declare i bigint default 0;
   declare IGBT_TemperatureTagID,DW_OutputTagID,DC_Bus_TagID,DrawworksSpeedTagID,HookloadTagID,BlockHeightTagID,TorqueTagID,DW_CurrentTagID,DW_FrequencyTagID int;
   declare IGBT_TemperatureUOM,DW_OutputUOM,DC_BusUOM,DrawworksSpeedUOM,HookloadUOM,BlockHeightUOM,TorqueUOM,DW_CurrentUOM,DW_FrequencyUOM varchar(50);
   declare Partition1,Partition2 varchar(40);
   declare previous_IGBTtemp,previous_dwOutput,previous_dcBus,previous_Hookload,previous_dwSpeed,previous_dwCurrent,previous_dwFrequency,previous_Torque,previous_BlockHeight float;

   drop temporary table if exists `temp_sqlth_data_1`;
   drop temporary table if exists `temp_sqlth_data_2`;
   drop temporary table if exists `temp_sqlth_data_3`;
   drop temporary table if exists `temp_sqlth_data_4`;
   drop temporary table if exists `temp_sqlth_data_5`;
   drop temporary table if exists `temp_sqlth_data_6`;
   drop temporary table if exists `temp_sqlth_data_7`;
   drop temporary table if exists `temp_sqlth_data_8`;
   drop temporary table if exists `temp_sqlth_data_9`;
   
   set start_tstamp=round(start_tstamp,-4);
   set stop_tstamp=round(stop_tstamp,-4);
   
   if ((stop_tstamp-start_tstamp)>2419200000) then
      /* if more than 28 days of duration request, only give them 28 days */
      set stop_tstamp=start_tstamp+2419200000;
   end if;
   
   select pname into Partition1 from sqlth_partitions where start_tstamp>=start_time and start_tstamp<end_time;
   select concat('create temporary table temp_sqlth_data_1 select t_stamp,tagid,floatvalue,intvalue from ',Partition1,' where t_stamp>=',start_tstamp,' and t_stamp<=',stop_tstamp) into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select pname into Partition2 from sqlth_partitions where (stop_tstamp>=start_time and stop_tstamp<end_time) and (start_tstamp<start_time);
   if (isnull(Partition2)=0) then
      select concat('insert into temp_sqlth_data_1 select t_stamp,tagid,floatvalue,intvalue from ',Partition2,' where t_stamp>=',start_tstamp,' and t_stamp<=',stop_tstamp) into @s;
      prepare stmt from @s; execute stmt; deallocate prepare stmt;
   end if;

   set i = start_tstamp;
   drop temporary table if exists dateRangeData;
   create temporary table dateRangeData(tenseconds_epoch bigint);
   while i <= stop_tstamp do
      insert into dateRangeData values(i);
      set i = i + 10000;
   end while;

   select te.id,tags.units into IGBT_TemperatureTagID,IGBT_TemperatureUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='IGBT Temperature' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into DW_OutputTagID,DW_OutputUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='DW Output Voltage' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into DC_Bus_TagID,DC_BusUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='DW DC Bus Voltage' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into HookloadTagID,HookloadUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Hookload' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into BlockHeightTagID,BlockHeightUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Block Height' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into TorqueTagID,TorqueUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Top Drive Torque' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into DrawworksSpeedTagID,DrawworksSpeedUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='DW Speed' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into DW_CurrentTagID,DW_CurrentUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='DW Current' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into DW_FrequencyTagID,DW_FrequencyUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='DW Frequency' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);

   -- due to a MySQL limitation, have to copy the temp_sqlth_data1 table several times as a temporary table is only allowed to referenced once in a query
   create temporary table temp_sqlth_data_2 select * from temp_sqlth_data_1 where tagid=DW_OutputTagID;
   create temporary table temp_sqlth_data_3 select * from temp_sqlth_data_1 where tagid=DC_Bus_TagID;
   create temporary table temp_sqlth_data_4 select * from temp_sqlth_data_1 where tagid=HookloadTagID;
   create temporary table temp_sqlth_data_5 select * from temp_sqlth_data_1 where tagid=BlockHeightTagID;
   create temporary table temp_sqlth_data_6 select * from temp_sqlth_data_1 where tagid=TorqueTagID;
   create temporary table temp_sqlth_data_7 select * from temp_sqlth_data_1 where tagid=DrawworksSpeedTagID;
   create temporary table temp_sqlth_data_8 select * from temp_sqlth_data_1 where tagid=DW_CurrentTagID;
   create temporary table temp_sqlth_data_9 select * from temp_sqlth_data_1 where tagid=DW_FrequencyTagID;
   delete from temp_sqlth_data_1 where tagid!=IGBT_TemperatureTagID;
   
   drop temporary table if exists temp_results;
   create temporary table temp_results
   select dateRangeData.tenseconds_epoch,IGBT_temp.tagvalue as IGBT_temp,dw_output.tagvalue as dw_output,DC_Bus.tagvalue as DC_Bus,hookload.tagvalue as Hookload
         ,BlockHeight.tagvalue as BlockHeight,Torque.tagvalue as Torque,dw_speed.tagvalue as dw_speed,dw_current.tagvalue as dw_current,dw_frequency.tagvalue as dw_frequency
         ,IGBT_TemperatureUOM,DW_OutputUOM,DC_BusUOM,HookloadUOM,BlockHeightUOM,TorqueUOM,DrawworksSpeedUOM,DW_CurrentUOM,DW_FrequencyUOM
     from dateRangeData
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_1 data group by round(data.t_stamp,-4)) IGBT_temp on dateRangeData.tenseconds_epoch=IGBT_temp.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_2 data group by round(data.t_stamp,-4)) dw_output on dateRangeData.tenseconds_epoch=dw_output.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_3 data group by round(data.t_stamp,-4)) DC_Bus on dateRangeData.tenseconds_epoch=DC_Bus.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_4 data group by round(data.t_stamp,-4)) hookload on dateRangeData.tenseconds_epoch=hookload.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_5 data group by round(data.t_stamp,-4)) BlockHeight on dateRangeData.tenseconds_epoch=BlockHeight.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_6 data group by round(data.t_stamp,-4)) Torque on dateRangeData.tenseconds_epoch=Torque.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_7 data group by round(data.t_stamp,-4)) dw_speed on dateRangeData.tenseconds_epoch=dw_speed.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_8 data group by round(data.t_stamp,-4)) dw_current on dateRangeData.tenseconds_epoch=dw_current.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_9 data group by round(data.t_stamp,-4)) dw_frequency on dateRangeData.tenseconds_epoch=dw_frequency.nearest10seconds_epoch
	;

   select concat('update temp_results set IGBT_temp=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',IGBT_TemperatureTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',IGBT_TemperatureTagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and IGBT_temp is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set dw_output=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',DW_OutputTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',DW_OutputTagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and dw_output is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set DC_Bus=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',DC_Bus_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',DC_Bus_TagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and DC_Bus is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set Hookload=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',HookloadTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',HookloadTagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and Hookload is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set BlockHeight=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',BlockHeightTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',BlockHeightTagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and BlockHeight is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set Torque=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',TorqueTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',TorqueTagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and Torque is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set dw_speed=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',DrawworksSpeedTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',DrawworksSpeedTagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and dw_speed is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set dw_current=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',DW_CurrentTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',DW_CurrentTagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and dw_current is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set dw_frequency=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',DW_FrequencyTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',DW_FrequencyTagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and dw_frequency is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
     
   set bigintCounter = start_tstamp;
   while bigintCounter<=stop_tstamp do
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

   drop temporary table if exists `temp_sqlth_data_1`;
   drop temporary table if exists `temp_sqlth_data_2`;
   drop temporary table if exists `temp_sqlth_data_3`;
   drop temporary table if exists `temp_sqlth_data_4`;
   drop temporary table if exists `temp_sqlth_data_5`;
   drop temporary table if exists `temp_sqlth_data_6`;
   drop temporary table if exists `temp_sqlth_data_7`;
   drop temporary table if exists `temp_sqlth_data_8`;
   drop temporary table if exists `temp_sqlth_data_9`;
   
END$$
DELIMITER ;
