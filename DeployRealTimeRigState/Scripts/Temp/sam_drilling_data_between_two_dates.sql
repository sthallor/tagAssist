CREATE DEFINER=`root`@`localhost` PROCEDURE `sam_drilling_data_between_two_dates`(IN `start_tstamp` BIGINT, IN `stop_tstamp` BIGINT)
	LANGUAGE SQL
	NOT DETERMINISTIC
	CONTAINS SQL
	SQL SECURITY DEFINER
	COMMENT ''
BEGIN
   declare bigintCounter bigint default 0;
   declare i bigint default 0;
   declare HoleDepthTagID,TorqueTagID,WeightOnBitTagID,RPM_TagID,DifferentialPressure_TagID,ROP_TagID,MSE_TagID int;
   declare HoleDepthUOM,TorqueUOM,WeightOnBitUOM,ROP_UOM,RPM_UOM,DP_UOM,MSE_UOM varchar(50);
   declare Partition1,Partition2 varchar(40);
   declare previous_HoleDepth,previous_ROP,previous_RPM,previous_Torque,previous_WOB,previous_MSE,previous_DP float;
   declare previous_MP1,previous_MP2 int;

   drop temporary table if exists `temp_sqlth_data_1`;
   drop temporary table if exists `temp_sqlth_data_2`;
   drop temporary table if exists `temp_sqlth_data_3`;
   drop temporary table if exists `temp_sqlth_data_4`;
   drop temporary table if exists `temp_sqlth_data_5`;
   drop temporary table if exists `temp_sqlth_data_6`;
   drop temporary table if exists `temp_sqlth_data_7`;
   
   set start_tstamp=ceil(start_tstamp/1000)*1000; /* previous 1 second interval */
   set stop_tstamp=round(stop_tstamp,-3);         /* rounded to the nearest 1 second interval */
   
   if ((stop_tstamp-start_tstamp)>2419200000) then
      /* if more than 28 days of duration request, only give them 28 days */
      set stop_tstamp=start_tstamp+2419200000;
   end if;
     
   /* Get TagID and UOM name for each measurement and store in variables */
   select te.id,tags.units into HoleDepthTagID,HoleDepthUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Hole Depth' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into TorqueTagID,TorqueUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Top Drive Torque' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into WeightOnBitTagID,WeightOnBitUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Weight On Bit' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into RPM_TagID,RPM_UOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Top Drive RPM' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into DifferentialPressure_TagID,DP_UOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Differential Pressure' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into ROP_TagID,ROP_UOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='ROP' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into MSE_TagID,MSE_UOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='MSE' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   
   /* Using the sqlth_partitions table, grab data for all tags across requested time range, plus previous 5 minutes. */
   /* Getting previous 5 minutes should ensure we find the first value for the first record.                         */
   select pname into Partition1 from sqlth_partitions where start_tstamp-300000>=start_time and start_tstamp-300000<end_time;
   select concat('create temporary table temp_sqlth_data_1 select t_stamp,tagid,floatvalue,intvalue from ',Partition1,' where t_stamp>=',start_tstamp-300000,' and t_stamp<=',stop_tstamp,' and tagid in (',HoleDepthTagID,',',TorqueTagID,',',WeightOnBitTagID,',',RPM_TagID,',',DifferentialPressure_TagID,',',ROP_TagID,',',MSE_TagID,')') into @s;

   prepare stmt from @s; execute stmt; deallocate prepare stmt;
      select pname into Partition2 from sqlth_partitions where (stop_tstamp>=start_time and stop_tstamp<end_time) and (start_tstamp<start_time);
   if (isnull(Partition2)=0) then
      select concat('insert into temp_sqlth_data_1 select t_stamp,tagid,floatvalue,intvalue from ',Partition2,' where t_stamp>=',start_tstamp-300000,' and t_stamp<=',stop_tstamp,' and tagid in (',HoleDepthTagID,',',TorqueTagID,',',WeightOnBitTagID,',',RPM_TagID,',',DifferentialPressure_TagID,',',ROP_TagID,',',MSE_TagID,')') into @s;
      prepare stmt from @s; execute stmt; deallocate prepare stmt;
   end if;
   
   /* Prime a table with 1 second increments for the requested time range */
   set i = start_tstamp;
   drop temporary table if exists rigStateDateRangeData;
   create temporary table rigStateDateRangeData(onesecond_epoch bigint primary key);
   while i <= stop_tstamp do
      insert into rigStateDateRangeData values(i);
      set i = i + 1000;
   end while;

   -- due to a MySQL limitation, have to copy the temp_sqlth_data1 table several times as a temporary table is only allowed to referenced once in a query
   create temporary table temp_sqlth_data_2 (index s2 (t_stamp)) select * from temp_sqlth_data_1 where tagid=HoleDepthTagID;
   create temporary table temp_sqlth_data_3 (index s3 (t_stamp)) select * from temp_sqlth_data_1 where tagid=TorqueTagID;
   create temporary table temp_sqlth_data_4 (index s4 (t_stamp)) select * from temp_sqlth_data_1 where tagid=WeightOnBitTagID;
   create temporary table temp_sqlth_data_5 (index s5 (t_stamp)) select * from temp_sqlth_data_1 where tagid=RPM_TagID;
   create temporary table temp_sqlth_data_6 (index s6 (t_stamp)) select * from temp_sqlth_data_1 where tagid=DifferentialPressure_TagID;
   create temporary table temp_sqlth_data_7 (index s7 (t_stamp)) select * from temp_sqlth_data_1 where tagid=ROP_TagID;
   delete from temp_sqlth_data_1 where tagid!=MSE_TagID;
      
    
   /* Using cached data captured a couple of steps above, find maximum values for each tag grouped by each 1 second interval in the */
   /* requested time range. */
   drop temporary table if exists temp_RigState_results;
   create temporary table temp_RigState_results (index tempIdx (onesecond_epoch))
   select rigStateDateRangeData.onesecond_epoch,DifferentialPressure.tagvalue as DifferentialPressure,HoleDepth.tagvalue as HoleDepth
         ,MSE.tagvalue as MSE,Torque.tagvalue as Torque,wob.tagvalue as WeightOnBit,rop.tagvalue as ROP,rpm.tagvalue as RPM
         ,HoleDepthUOM,TorqueUOM,WeightOnBitUOM,ROP_UOM,RPM_UOM,DP_UOM,MSE_UOM
	 from rigStateDateRangeData
          left outer join (select ceil(data.t_stamp/1000)*1000 as nearest1second_epoch,max(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_6 data group by ceil(data.t_stamp/1000)*1000) DifferentialPressure on rigStateDateRangeData.onesecond_epoch=DifferentialPressure.nearest1second_epoch
          left outer join (select ceil(data.t_stamp/1000)*1000 as nearest1second_epoch,max(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_2 data group by ceil(data.t_stamp/1000)*1000) HoleDepth on rigStateDateRangeData.onesecond_epoch=HoleDepth.nearest1second_epoch
          left outer join (select ceil(data.t_stamp/1000)*1000 as nearest1second_epoch,max(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_1 data group by ceil(data.t_stamp/1000)*1000) MSE on rigStateDateRangeData.onesecond_epoch=MSE.nearest1second_epoch
          left outer join (select ceil(data.t_stamp/1000)*1000 as nearest1second_epoch,max(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_3 data group by ceil(data.t_stamp/1000)*1000) Torque on rigStateDateRangeData.onesecond_epoch=Torque.nearest1second_epoch
          left outer join (select ceil(data.t_stamp/1000)*1000 as nearest1second_epoch,max(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_4 data group by ceil(data.t_stamp/1000)*1000) wob on rigStateDateRangeData.onesecond_epoch=wob.nearest1second_epoch
          left outer join (select ceil(data.t_stamp/1000)*1000 as nearest1second_epoch,max(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_7 data group by ceil(data.t_stamp/1000)*1000) rop on rigStateDateRangeData.onesecond_epoch=rop.nearest1second_epoch
          left outer join (select ceil(data.t_stamp/1000)*1000 as nearest1second_epoch,max(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_5 data group by ceil(data.t_stamp/1000)*1000) rpm on rigStateDateRangeData.onesecond_epoch=rpm.nearest1second_epoch
   ;
   
   /* If first record has a NULL value, looks back in the cached data to find the last value for each tag and saved it to the first record. */
   select concat('update temp_RigState_results set HoleDepth=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',HoleDepthTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',HoleDepthTagID,' and t_stamp<=',start_tstamp,')) where onesecond_epoch=',start_tstamp,' and HoleDepth is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set MSE=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',MSE_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',MSE_TagID,' and t_stamp<=',start_tstamp,')) where onesecond_epoch=',start_tstamp,' and MSE is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set DifferentialPressure=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',DifferentialPressure_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',DifferentialPressure_TagID,' and t_stamp<=',start_tstamp,')) where onesecond_epoch=',start_tstamp,' and DifferentialPressure is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set Torque=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',TorqueTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',TorqueTagID,' and t_stamp<=',start_tstamp,')) where onesecond_epoch=',start_tstamp,' and Torque is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set WeightOnBit=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',WeightOnBitTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',WeightOnBitTagID,' and t_stamp<=',start_tstamp,')) where onesecond_epoch=',start_tstamp,' and WeightOnBit is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set ROP=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',ROP_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',ROP_TagID,' and t_stamp<=',start_tstamp,')) where onesecond_epoch=',start_tstamp,' and ROP is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set RPM=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',RPM_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',RPM_TagID,' and t_stamp<=',start_tstamp,')) where onesecond_epoch=',start_tstamp,' and RPM is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   
   set bigintCounter = start_tstamp;
   while bigintCounter<=stop_tstamp do
      /* fill in NULL values that might be scattered in result set, by looking farther back in cached data than just the previous 1 second */
      update temp_RigState_results set DifferentialPressure=(select ifnull(floatvalue,intvalue) from temp_sqlth_data_6 where t_stamp<bigIntCounter order by t_stamp desc limit 1) where onesecond_epoch=bigIntCounter and DifferentialPressure is null;
      update temp_RigState_results set HoleDepth=(select ifnull(floatvalue,intvalue) from temp_sqlth_data_2 where t_stamp<bigIntCounter order by t_stamp desc limit 1) where onesecond_epoch=bigIntCounter and HoleDepth is null;
      update temp_RigState_results set MSE=(select ifnull(floatvalue,intvalue) from temp_sqlth_data_1 where t_stamp<bigIntCounter order by t_stamp desc limit 1) where onesecond_epoch=bigIntCounter and MSE is null;
      update temp_RigState_results set Torque=(select ifnull(floatvalue,intvalue) from temp_sqlth_data_3 where t_stamp<bigIntCounter order by t_stamp desc limit 1) where onesecond_epoch=bigIntCounter and Torque is null;
      update temp_RigState_results set WeightOnBit=(select ifnull(floatvalue,intvalue) from temp_sqlth_data_4 where t_stamp<bigIntCounter order by t_stamp desc limit 1) where onesecond_epoch=bigIntCounter and WeightOnBit is null;
      update temp_RigState_results set ROP=(select ifnull(floatvalue,intvalue) from temp_sqlth_data_7 where t_stamp<bigIntCounter order by t_stamp desc limit 1) where onesecond_epoch=bigIntCounter and ROP is null;
      update temp_RigState_results set RPM=(select ifnull(floatvalue,intvalue) from temp_sqlth_data_5 where t_stamp<bigIntCounter order by t_stamp desc limit 1) where onesecond_epoch=bigIntCounter and RPM is null;
      
      set bigintCounter = bigintCounter + 1000;

   end while;
   select * from temp_RigState_results order by onesecond_epoch;

   drop temporary table if exists `temp_sqlth_data_1`;
   drop temporary table if exists `temp_sqlth_data_2`;
   drop temporary table if exists `temp_sqlth_data_3`;
   drop temporary table if exists `temp_sqlth_data_4`;
   drop temporary table if exists `temp_sqlth_data_5`;
   drop temporary table if exists `temp_sqlth_data_6`;
   drop temporary table if exists `temp_sqlth_data_7`;

END