drop procedure if exists `get_last_5minutes_drilling_data`;
DELIMITER $$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_last_5minutes_drilling_data`()
    DETERMINISTIC
BEGIN
   declare howManySecondsToLiveInPast int default 20;
   declare 5minutesAgo,rightNow,bigintCounter bigint default 0;
   declare i int default 0;
   declare HoleDepthTagID,BitDepthTagID,SP_TagID,MP1_TagID,MP2_TagID,HookloadTagID,BlockHeightTagID,TorqueTagID,WeightOnBitTagID,ROP_TagID,RPM_TagID int;
   declare HoleDepthUOM,BitDepthUOM,StandpipePressureUOM,MudPump1SPM_UOM,MudPump2SPM_UOM,HookloadUOM,BlockHeightUOM,TorqueUOM,WeightOnBitUOM,ROP_UOM,RPM_UOM varchar(50);
   declare Partition1,Partition2 varchar(40);
   declare previous_HoleDepth,previous_BitDepth,previous_BlockHeight,previous_Hookload,previous_StandpipePressure,previous_ROP,previous_RPM,previous_Torque,previous_WOB float;
   declare previous_MP1,previous_MP2 int;

   drop temporary table if exists `temp_sqlth_data_1`;
   drop temporary table if exists `temp_sqlth_data_2`;
   drop temporary table if exists `temp_sqlth_data_3`;
   drop temporary table if exists `temp_sqlth_data_4`;
   drop temporary table if exists `temp_sqlth_data_5`;
   drop temporary table if exists `temp_sqlth_data_6`;
   drop temporary table if exists `temp_sqlth_data_7`;
   drop temporary table if exists `temp_sqlth_data_8`;
   drop temporary table if exists `temp_sqlth_data_9`;
   drop temporary table if exists `temp_sqlth_data_10`;
   drop temporary table if exists `temp_sqlth_data_11`;
   drop temporary table if exists `base_1`;
   drop temporary table if exists `base_2`;
   drop temporary table if exists `base_3`;
   drop temporary table if exists `base_4`;
   drop temporary table if exists `base_5`;
   drop temporary table if exists `base_6`;
   drop temporary table if exists `base_7`;
   drop temporary table if exists `base_8`;
   drop temporary table if exists `base_9`;
   drop temporary table if exists `base_10`;
   drop temporary table if exists `base_11`;

   set 5minutesAgo = round((unix_timestamp()-300)*1000,-4)-10000-(howManySecondsToLiveInPast*1000);  /* Now minus 5 minutes, multiply by 1000 for milliseconds; minus 10 seconds to get 5 minutes + 10 seconds */
   set rightNow = 5minutesAgo + 310000; /* don't add howManySecondsToLiveInPast, we want to live 'x' seconds in the past */
   select pname into Partition1 from sqlth_partitions where 5minutesAgo>=start_time and 5minutesAgo<end_time;
   select concat('create temporary table temp_sqlth_data_1 select t_stamp,tagid,floatvalue,intvalue from ',Partition1,' where t_stamp>=',5minutesAgo) into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select pname into Partition2 from sqlth_partitions where (rightNow>=start_time and rightNow<end_time) and (5minutesAgo<start_time);
   if (isnull(Partition2)=0) then
      select concat('insert into temp_sqlth_data_1 select t_stamp,tagid,floatvalue,intvalue from ',Partition2,' where t_stamp>=',5minutesAgo) into @s;
      prepare stmt from @s; execute stmt; deallocate prepare stmt;
   end if;
   
   set i = 0;
   drop temporary table if exists 5minutes;
   create temporary table 5minutes(tenseconds_epoch bigint);
   while i <= 30 do
      insert into 5minutes values(5minutesAgo+(i*10000));
      set i = i + 1;
   end while;

   select te.id,tags.units into HoleDepthTagID,HoleDepthUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Hole Depth' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into BitDepthTagID,BitDepthUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Bit Depth' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into SP_TagID,StandpipePressureUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Standpipe Pressure' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into MP1_TagID,MudPump1SPM_UOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='MP1 SPM' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into MP2_TagID,MudPump2SPM_UOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='MP2 SPM' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into HookloadTagID,HookloadUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Hookload' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into BlockHeightTagID,BlockHeightUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Block Height' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into TorqueTagID,TorqueUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Top Drive Torque' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into WeightOnBitTagID,WeightOnBitUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Weight On Bit' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into ROP_TagID,ROP_UOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='ROP' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);
   select te.id,tags.units into RPM_TagID,RPM_UOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Top Drive RPM' and rightNow>=te.created and rightNow<ifnull(te.retired,4102444800000);

-- due to a MySQL limitation, have to copy the temp_sqlth_data1 table several times as a temporary table is only allowed to referenced once in a query
   create temporary table temp_sqlth_data_2 select * from temp_sqlth_data_1 where tagid=HoleDepthTagID;
   create temporary table base_2 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_2 group by round(t_stamp,-4);
   
   create temporary table temp_sqlth_data_3 select * from temp_sqlth_data_1 where tagid=BitDepthTagID;
   create temporary table base_3 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_3 group by round(t_stamp,-4);
   
   create temporary table temp_sqlth_data_4 select * from temp_sqlth_data_1 where tagid=HookloadTagID;
   create temporary table base_4 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_4 group by round(t_stamp,-4);
   
   create temporary table temp_sqlth_data_5 select * from temp_sqlth_data_1 where tagid=MP1_TagID;
   create temporary table base_5 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_5 group by round(t_stamp,-4);
   
   create temporary table temp_sqlth_data_6 select * from temp_sqlth_data_1 where tagid=MP2_TagID;
   create temporary table base_6 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_6 group by round(t_stamp,-4);
   
   create temporary table temp_sqlth_data_7 select * from temp_sqlth_data_1 where tagid=BlockHeightTagID;
   create temporary table base_7 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_7 group by round(t_stamp,-4);
   
   create temporary table temp_sqlth_data_8 select * from temp_sqlth_data_1 where tagid=TorqueTagID;
   create temporary table base_8 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_8 group by round(t_stamp,-4);
   
   create temporary table temp_sqlth_data_9 select * from temp_sqlth_data_1 where tagid=WeightOnBitTagID;
   create temporary table base_9 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_9 group by round(t_stamp,-4);
   
   create temporary table temp_sqlth_data_10 select * from temp_sqlth_data_1 where tagid=ROP_TagID;
   create temporary table base_10 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_10 group by round(t_stamp,-4);
   
   create temporary table temp_sqlth_data_11 select * from temp_sqlth_data_1 where tagid=RPM_TagID;
   create temporary table base_11 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_11 group by round(t_stamp,-4);
   
   delete from temp_sqlth_data_1 where tagid!=SP_TagID;
   create temporary table base_1 select round(t_stamp,-4) as nearest10seconds_epoch,min(t_stamp) as 1st_tstamp from temp_sqlth_data_1 group by round(t_stamp,-4);
	
   drop temporary table if exists temp_results;
   create temporary table temp_results
   select 5minutes.tenseconds_epoch,StandpipePressure.tagvalue as StandpipePressure,HoleDepth.tagvalue as HoleDepth,BitDepth.tagvalue as BitDepth,hookload.tagvalue as Hookload
         ,mp1.tagvalue as mp1_spm,mp2.tagvalue as mp2_spm,BlockHeight.tagvalue as BlockHeight,Torque.tagvalue as Torque,wob.tagvalue as WeightOnBit,rop.tagvalue as ROP,rpm.tagvalue as RPM
         ,StandpipePressureUOM,HoleDepthUOM,BitDepthUOM,HookloadUOM,MudPump1SPM_UOM,MudPump2SPM_UOM,BlockHeightUOM,TorqueUOM,WeightOnBitUOM,ROP_UOM,RPM_UOM
	  from 5minutes
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_1 base inner join temp_sqlth_data_1 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=5minutesAgo) StandpipePressure on 5minutes.tenseconds_epoch=StandpipePressure.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_2 base inner join temp_sqlth_data_2 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=5minutesAgo) HoleDepth on 5minutes.tenseconds_epoch=HoleDepth.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_3 base inner join temp_sqlth_data_3 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=5minutesAgo) BitDepth on 5minutes.tenseconds_epoch=BitDepth.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_4 base inner join temp_sqlth_data_4 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=5minutesAgo) hookload on 5minutes.tenseconds_epoch=hookload.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_5 base inner join temp_sqlth_data_5 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=5minutesAgo) mp1 on 5minutes.tenseconds_epoch=mp1.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_6 base inner join temp_sqlth_data_6 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=5minutesAgo) mp2 on 5minutes.tenseconds_epoch=mp2.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_7 base inner join temp_sqlth_data_7 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=5minutesAgo) BlockHeight on 5minutes.tenseconds_epoch=BlockHeight.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_8 base inner join temp_sqlth_data_8 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=5minutesAgo) Torque on 5minutes.tenseconds_epoch=Torque.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_9 base inner join temp_sqlth_data_9 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=5minutesAgo) wob on 5minutes.tenseconds_epoch=wob.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_10 base inner join temp_sqlth_data_10 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=5minutesAgo) rop on 5minutes.tenseconds_epoch=rop.nearest10seconds_epoch
          left outer join (select base.nearest10seconds_epoch,ifnull(data.floatvalue,data.intvalue) as tagvalue from base_11 base inner join temp_sqlth_data_11 data on base.1st_tstamp=data.t_stamp where data.t_stamp>=5minutesAgo) rpm on 5minutes.tenseconds_epoch=rpm.nearest10seconds_epoch
   ;
   
   select concat('update temp_results set HoleDepth=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',HoleDepthTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',HoleDepthTagID,' and t_stamp<=',5minutesAgo,')) where tenseconds_epoch=',5minutesAgo,' and HoleDepth is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set BitDepth=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',BitDepthTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',BitDepthTagID,' and t_stamp<=',5minutesAgo,')) where tenseconds_epoch=',5minutesAgo,' and BitDepth is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set StandpipePressure=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',SP_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',SP_TagID,' and t_stamp<=',5minutesAgo,')) where tenseconds_epoch=',5minutesAgo,' and StandpipePressure is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set Hookload=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',HookloadTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',HookloadTagID,' and t_stamp<=',5minutesAgo,')) where tenseconds_epoch=',5minutesAgo,' and Hookload is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set BlockHeight=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',BlockHeightTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',BlockHeightTagID,' and t_stamp<=',5minutesAgo,')) where tenseconds_epoch=',5minutesAgo,' and BlockHeight is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set mp1_spm=(select intvalue from ',Partition1,' where tagid=',MP1_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',MP1_TagID,' and t_stamp<=',5minutesAgo,')) where tenseconds_epoch=',5minutesAgo,' and mp1_spm is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set mp2_spm=(select intvalue from ',Partition1,' where tagid=',MP2_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',MP2_TagID,' and t_stamp<=',5minutesAgo,')) where tenseconds_epoch=',5minutesAgo,' and mp2_spm is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set Torque=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',TorqueTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',TorqueTagID,' and t_stamp<=',5minutesAgo,')) where tenseconds_epoch=',5minutesAgo,' and Torque is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set WeightOnBit=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',WeightOnBitTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',WeightOnBitTagID,' and t_stamp<=',5minutesAgo,')) where tenseconds_epoch=',5minutesAgo,' and WeightOnBit is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set ROP=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',ROP_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',ROP_TagID,' and t_stamp<=',5minutesAgo,')) where tenseconds_epoch=',5minutesAgo,' and ROP is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_results set RPM=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',RPM_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',RPM_TagID,' and t_stamp<=',5minutesAgo,')) where tenseconds_epoch=',5minutesAgo,' and RPM is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;

   set bigintCounter = 5minutesAgo;
   while bigintCounter<=(5minutesAgo + 310000) do
      update temp_results set RPM=previous_RPM where tenseconds_epoch=bigintCounter and RPM is null;
      update temp_results set HoleDepth=previous_HoleDepth where tenseconds_epoch=bigintCounter and HoleDepth is null;
      update temp_results set BitDepth=previous_BitDepth where tenseconds_epoch=bigintCounter and BitDepth is null;
      update temp_results set StandpipePressure=previous_StandpipePressure where tenseconds_epoch=bigintCounter and StandpipePressure is null;
      update temp_results set Hookload=previous_Hookload where tenseconds_epoch=bigintCounter and Hookload is null;
      update temp_results set BlockHeight=previous_BlockHeight where tenseconds_epoch=bigintCounter and BlockHeight is null;
      update temp_results set mp1_spm=previous_MP1 where tenseconds_epoch=bigintCounter and mp1_spm is null;
      update temp_results set mp2_spm=previous_MP2 where tenseconds_epoch=bigintCounter and mp2_spm is null;
      update temp_results set Torque=previous_Torque where tenseconds_epoch=bigintCounter and Torque is null;
      update temp_results set WeightOnBit=previous_WOB where tenseconds_epoch=bigintCounter and WeightOnBit is null;
      update temp_results set ROP=previous_ROP where tenseconds_epoch=bigintCounter and ROP is null;
      
      select RPM into previous_RPM from temp_results where tenseconds_epoch=bigintCounter;
      select HoleDepth into previous_HoleDepth from temp_results where tenseconds_epoch=bigintCounter;
      select BitDepth into previous_BitDepth from temp_results where tenseconds_epoch=bigintCounter;
      select StandpipePressure into previous_StandpipePressure from temp_results where tenseconds_epoch=bigintCounter;
      select Hookload into previous_Hookload from temp_results where tenseconds_epoch=bigintCounter;
      select BlockHeight into previous_BlockHeight from temp_results where tenseconds_epoch=bigintCounter;
      select mp1_spm into previous_MP1 from temp_results where tenseconds_epoch=bigintCounter;
      select mp2_spm into previous_MP2 from temp_results where tenseconds_epoch=bigintCounter;
      select Torque into previous_Torque from temp_results where tenseconds_epoch=bigintCounter;
      select WeightOnBit into previous_WOB from temp_results where tenseconds_epoch=bigintCounter;
      select ROP into previous_ROP from temp_results where tenseconds_epoch=bigintCounter;

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
   drop temporary table if exists `temp_sqlth_data_10`;
   drop temporary table if exists `temp_sqlth_data_11`;
   drop temporary table if exists `base_1`;
   drop temporary table if exists `base_2`;
   drop temporary table if exists `base_3`;
   drop temporary table if exists `base_4`;
   drop temporary table if exists `base_5`;
   drop temporary table if exists `base_6`;
   drop temporary table if exists `base_7`;
   drop temporary table if exists `base_8`;
   drop temporary table if exists `base_9`;
   drop temporary table if exists `base_10`;
   drop temporary table if exists `base_11`;
END$$
DELIMITER ;
