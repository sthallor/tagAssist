drop procedure if exists `get_drilling_data_between_two_dates`;
DELIMITER $$
CREATE DEFINER=`root`@`localhost` PROCEDURE `get_drilling_data_between_two_dates`(IN `start_tstamp` BIGINT, IN `stop_tstamp` BIGINT)
BEGIN
   declare bigintCounter bigint default 0;
   declare i bigint default 0;
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
   drop temporary table if exists rigStateDateRangeData;
   create temporary table rigStateDateRangeData(tenseconds_epoch bigint);
   while i <= stop_tstamp do
      insert into rigStateDateRangeData values(i);
      set i = i + 10000;
   end while;

   select te.id,tags.units into HoleDepthTagID,HoleDepthUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Hole Depth' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into BitDepthTagID,BitDepthUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Bit Depth' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into SP_TagID,StandpipePressureUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Standpipe Pressure' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into MP1_TagID,MudPump1SPM_UOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='MP1 SPM' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into MP2_TagID,MudPump2SPM_UOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='MP2 SPM' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into HookloadTagID,HookloadUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Hookload' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into BlockHeightTagID,BlockHeightUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Block Height' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into TorqueTagID,TorqueUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Top Drive Torque' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into WeightOnBitTagID,WeightOnBitUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Weight On Bit' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into ROP_TagID,ROP_UOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='ROP' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id,tags.units into RPM_TagID,RPM_UOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Top Drive RPM' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);

   -- due to a MySQL limitation, have to copy the temp_sqlth_data1 table several times as a temporary table is only allowed to referenced once in a query
   create temporary table temp_sqlth_data_2 select * from temp_sqlth_data_1 where tagid=HoleDepthTagID;
   create temporary table temp_sqlth_data_3 select * from temp_sqlth_data_1 where tagid=BitDepthTagID;
   create temporary table temp_sqlth_data_4 select * from temp_sqlth_data_1 where tagid=HookloadTagID;
   create temporary table temp_sqlth_data_5 select * from temp_sqlth_data_1 where tagid=MP1_TagID;
   create temporary table temp_sqlth_data_6 select * from temp_sqlth_data_1 where tagid=MP2_TagID;
   create temporary table temp_sqlth_data_7 select * from temp_sqlth_data_1 where tagid=BlockHeightTagID;
   create temporary table temp_sqlth_data_8 select * from temp_sqlth_data_1 where tagid=TorqueTagID;
   create temporary table temp_sqlth_data_9 select * from temp_sqlth_data_1 where tagid=WeightOnBitTagID;
   create temporary table temp_sqlth_data_10 select * from temp_sqlth_data_1 where tagid=ROP_TagID;
   create temporary table temp_sqlth_data_11 select * from temp_sqlth_data_1 where tagid=RPM_TagID;
   delete from temp_sqlth_data_1 where tagid!=SP_TagID;
      
   drop temporary table if exists temp_RigState_results;
   create temporary table temp_RigState_results
   select rigStateDateRangeData.tenseconds_epoch,StandpipePressure.tagvalue as StandpipePressure,HoleDepth.tagvalue as HoleDepth,BitDepth.tagvalue as BitDepth,hookload.tagvalue as Hookload
         ,mp1.tagvalue as mp1_spm,mp2.tagvalue as mp2_spm,BlockHeight.tagvalue as BlockHeight,Torque.tagvalue as Torque,wob.tagvalue as WeightOnBit,rop.tagvalue as ROP,rpm.tagvalue as RPM
         ,StandpipePressureUOM,HoleDepthUOM,BitDepthUOM,HookloadUOM,MudPump1SPM_UOM,MudPump2SPM_UOM,BlockHeightUOM,TorqueUOM,WeightOnBitUOM,ROP_UOM,RPM_UOM
	 from rigStateDateRangeData
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_1 data group by round(data.t_stamp,-4)) StandpipePressure on rigStateDateRangeData.tenseconds_epoch=StandpipePressure.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_2 data group by round(data.t_stamp,-4)) HoleDepth on rigStateDateRangeData.tenseconds_epoch=HoleDepth.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_3 data group by round(data.t_stamp,-4)) BitDepth on rigStateDateRangeData.tenseconds_epoch=BitDepth.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_4 data group by round(data.t_stamp,-4)) hookload on rigStateDateRangeData.tenseconds_epoch=hookload.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_5 data group by round(data.t_stamp,-4)) mp1 on rigStateDateRangeData.tenseconds_epoch=mp1.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_6 data group by round(data.t_stamp,-4)) mp2 on rigStateDateRangeData.tenseconds_epoch=mp2.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_7 data group by round(data.t_stamp,-4)) BlockHeight on rigStateDateRangeData.tenseconds_epoch=BlockHeight.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_8 data group by round(data.t_stamp,-4)) Torque on rigStateDateRangeData.tenseconds_epoch=Torque.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_9 data group by round(data.t_stamp,-4)) wob on rigStateDateRangeData.tenseconds_epoch=wob.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_10 data group by round(data.t_stamp,-4)) rop on rigStateDateRangeData.tenseconds_epoch=rop.nearest10seconds_epoch
          left outer join (select round(data.t_stamp,-4) as nearest10seconds_epoch,avg(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_11 data group by round(data.t_stamp,-4)) rpm on rigStateDateRangeData.tenseconds_epoch=rpm.nearest10seconds_epoch
   ;
   
   select concat('update temp_RigState_results set HoleDepth=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',HoleDepthTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',HoleDepthTagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and HoleDepth is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set BitDepth=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',BitDepthTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',BitDepthTagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and BitDepth is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set StandpipePressure=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',SP_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',SP_TagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and StandpipePressure is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set Hookload=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',HookloadTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',HookloadTagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and Hookload is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set BlockHeight=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',BlockHeightTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',BlockHeightTagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and BlockHeight is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set mp1_spm=(select intvalue from ',Partition1,' where tagid=',MP1_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',MP1_TagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and mp1_spm is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set mp2_spm=(select intvalue from ',Partition1,' where tagid=',MP2_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',MP2_TagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and mp2_spm is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set Torque=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',TorqueTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',TorqueTagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and Torque is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set WeightOnBit=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',WeightOnBitTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',WeightOnBitTagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and WeightOnBit is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set ROP=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',ROP_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',ROP_TagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and ROP is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select concat('update temp_RigState_results set RPM=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',RPM_TagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',RPM_TagID,' and t_stamp<=',start_tstamp,')) where tenseconds_epoch=',start_tstamp,' and RPM is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
     
   set bigintCounter = start_tstamp;
   while bigintCounter<=stop_tstamp do
      update temp_RigState_results set RPM=previous_RPM where tenseconds_epoch=bigintCounter and RPM is null;
      update temp_RigState_results set HoleDepth=previous_HoleDepth where tenseconds_epoch=bigintCounter and HoleDepth is null;
      update temp_RigState_results set BitDepth=previous_BitDepth where tenseconds_epoch=bigintCounter and BitDepth is null;
      update temp_RigState_results set StandpipePressure=previous_StandpipePressure where tenseconds_epoch=bigintCounter and StandpipePressure is null;
      update temp_RigState_results set Hookload=previous_Hookload where tenseconds_epoch=bigintCounter and Hookload is null;
      update temp_RigState_results set BlockHeight=previous_BlockHeight where tenseconds_epoch=bigintCounter and BlockHeight is null;
      update temp_RigState_results set mp1_spm=previous_MP1 where tenseconds_epoch=bigintCounter and mp1_spm is null;
      update temp_RigState_results set mp2_spm=previous_MP2 where tenseconds_epoch=bigintCounter and mp2_spm is null;
      update temp_RigState_results set Torque=previous_Torque where tenseconds_epoch=bigintCounter and Torque is null;
      update temp_RigState_results set WeightOnBit=previous_WOB where tenseconds_epoch=bigintCounter and WeightOnBit is null;
      update temp_RigState_results set ROP=previous_ROP where tenseconds_epoch=bigintCounter and ROP is null;
      
      select RPM into previous_RPM from temp_RigState_results where tenseconds_epoch=bigintCounter;
      select HoleDepth into previous_HoleDepth from temp_RigState_results where tenseconds_epoch=bigintCounter;
      select BitDepth into previous_BitDepth from temp_RigState_results where tenseconds_epoch=bigintCounter;
      select StandpipePressure into previous_StandpipePressure from temp_RigState_results where tenseconds_epoch=bigintCounter;
      select Hookload into previous_Hookload from temp_RigState_results where tenseconds_epoch=bigintCounter;
      select BlockHeight into previous_BlockHeight from temp_RigState_results where tenseconds_epoch=bigintCounter;
      select mp1_spm into previous_MP1 from temp_RigState_results where tenseconds_epoch=bigintCounter;
      select mp2_spm into previous_MP2 from temp_RigState_results where tenseconds_epoch=bigintCounter;
      select Torque into previous_Torque from temp_RigState_results where tenseconds_epoch=bigintCounter;
      select WeightOnBit into previous_WOB from temp_RigState_results where tenseconds_epoch=bigintCounter;
      select ROP into previous_ROP from temp_RigState_results where tenseconds_epoch=bigintCounter;

      set bigintCounter = bigintCounter + 10000;

   end while;

   select * from temp_RigState_results order by tenseconds_epoch;

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
END$$
DELIMITER ;
