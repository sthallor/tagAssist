CREATE DEFINER=`root`@`localhost` PROCEDURE `sam_effective_toolface_between_two_dates_perSecond`(IN `start_tstamp` BIGINT, IN `stop_tstamp` BIGINT)
	LANGUAGE SQL
	NOT DETERMINISTIC
	CONTAINS SQL
	SQL SECURITY DEFINER
	COMMENT ''
BEGIN
   declare bigintCounter bigint default 0;
   declare i bigint default 0;
   declare HoleDepthTagID,ToolfaceTagID,QuillOscEnabledTagID,BitDepthTagID int;
   declare HoleDepthUOM varchar(50);
   declare Partition1,Partition2 varchar(40);
   declare previous_HoleDepth,previous_Toolface,previous_BitDepth float;
	declare previous_QuillOscEnabled int;

   declare current_HoleDepth,current_Toolface,current_BitDepth float;
   declare current_QO int;
   declare next_HoleDepth,next_Toolface,next_BitDepth float;
   declare next_QO int;   
   
   drop temporary table if exists `temp_sqlth_data_1`;
   drop temporary table if exists `temp_sqlth_data_2`;
   drop temporary table if exists `temp_sqlth_data_3`;
   drop temporary table if exists `temp_sqlth_data_4`;
   drop temporary table if exists `temp_final_results`;
	   
   set start_tstamp=ceil(start_tstamp/1000)*1000; /* previous 1 second interval */
   set stop_tstamp=round(stop_tstamp,-3);         /* rounded to the nearest 1 second interval */
   
   if ((stop_tstamp-start_tstamp)>2419200000) then
      /* if more than 28 days of duration request, only give them 28 days */
      set stop_tstamp=start_tstamp+2419200000;
   end if;
   
   /* Get TagID and UOM name for each measurement and store in variables */
   select te.id,tags.units into HoleDepthTagID,HoleDepthUOM from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Hole Depth' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id into BitDepthTagID from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Bit Depth' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id into ToolfaceTagID from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='Toolface' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);
   select te.id into QuillOscEnabledTagID from sqlth_te te inner join tags on te.tagpath=tags.tag_path where tags.display_name='QO Enabled' and stop_tstamp>=te.created and stop_tstamp<ifnull(te.retired,4102444800000);

   /* Using the sqlth_partitions table, grab data for all tags across requested time range, plus previous 5 minutes. */
   /* Getting previous 5 minutes should ensure we find the first value for the first record.                         */
   select pname into Partition1 from sqlth_partitions where start_tstamp-300000>=start_time and start_tstamp-300000<end_time;
   select concat('create temporary table temp_sqlth_data_1 select t_stamp,tagid,floatvalue,intvalue from ',Partition1,' where t_stamp>=',start_tstamp-300000,' and t_stamp<=',stop_tstamp,' and tagid in (',BitDepthTagID,',',HoleDepthTagID,',',ToolfaceTagID,',',QuillOscEnabledTagID,')') into @s;

   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   select pname into Partition2 from sqlth_partitions where (stop_tstamp>=start_time and stop_tstamp<end_time) and (start_tstamp<start_time);
   if (isnull(Partition2)=0) then
      select concat('insert into temp_sqlth_data_1 select t_stamp,tagid,floatvalue,intvalue from ',Partition2,' where t_stamp>=',start_tstamp-300000,' and t_stamp<=',stop_tstamp,' and tagid in (',BitDepthTagID,',',HoleDepthTagID,',',ToolfaceTagID,',',QuillOscEnabledTagID,')') into @s;
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
   create temporary table temp_sqlth_data_2 (index s2 (t_stamp)) select * from temp_sqlth_data_1 where tagid=ToolfaceTagID;
   create temporary table temp_sqlth_data_3 (index s3 (t_stamp)) select * from temp_sqlth_data_1 where tagid=QuillOscEnabledTagID;
   create temporary table temp_sqlth_data_4 (index s4 (t_stamp)) select * from temp_sqlth_data_1 where tagid=BitDepthTagID;
   delete from temp_sqlth_data_1 where tagid!=HoleDepthTagID;
   commit;
   
   /* Using cached data captured a couple of steps above, find maximum values for each tag grouped by each 1 second interval in the */
   /* requested time range. */
   drop temporary table if exists temp_RigState_results;
   create temporary table temp_RigState_results (index tempIdx (onesecond_epoch))
   select rigStateDateRangeData.onesecond_epoch,HoleDepth.tagvalue as HoleDepth
         ,Toolface.tagvalue as Toolface,QOenabled.tagvalue as QuillOscEnabled
         ,BitDepth.tagvalue as BitDepth
         ,HoleDepthUOM
	 from rigStateDateRangeData
          left outer join (select ceil(data.t_stamp/1000)*1000 as nearest1second_epoch,max(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_1 data group by ceil(data.t_stamp/1000)*1000) HoleDepth on rigStateDateRangeData.onesecond_epoch=HoleDepth.nearest1second_epoch
          left outer join (select ceil(data.t_stamp/1000)*1000 as nearest1second_epoch,max(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_2 data group by ceil(data.t_stamp/1000)*1000) Toolface on rigStateDateRangeData.onesecond_epoch=Toolface.nearest1second_epoch
          left outer join (select ceil(data.t_stamp/1000)*1000 as nearest1second_epoch,max(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_3 data group by ceil(data.t_stamp/1000)*1000) QOenabled on rigStateDateRangeData.onesecond_epoch=QOenabled.nearest1second_epoch
          left outer join (select ceil(data.t_stamp/1000)*1000 as nearest1second_epoch,max(ifnull(data.floatvalue,data.intvalue)) as tagvalue from temp_sqlth_data_4 data group by ceil(data.t_stamp/1000)*1000) BitDepth on rigStateDateRangeData.onesecond_epoch=BitDepth.nearest1second_epoch
   ;

   /* If first record has a NULL value, looks back in the cached data to find the last value for each tag and saved it to the first record. */
   select concat('update temp_RigState_results set HoleDepth=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',HoleDepthTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',HoleDepthTagID,' and t_stamp<=',start_tstamp,')) where onesecond_epoch=',start_tstamp,' and HoleDepth is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   commit;
   select concat('update temp_RigState_results set Toolface=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',ToolfaceTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',ToolfaceTagID,' and t_stamp<=',start_tstamp,')) where onesecond_epoch=',start_tstamp,' and Toolface is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   commit;
   select concat('update temp_RigState_results set QuillOscEnabled=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',QuillOscEnabledTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',QuillOscEnabledTagID,' and t_stamp<=',start_tstamp,')) where onesecond_epoch=',start_tstamp,' and QuillOscEnabled is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   commit;
   select concat('update temp_RigState_results set BitDepth=(select ifnull(floatvalue,intvalue) from ',Partition1,' where tagid=',BitDepthTagID,' and t_stamp=(select max(t_stamp) from ',Partition1,' where tagid=',BitDepthTagID,' and t_stamp<=',start_tstamp,')) where onesecond_epoch=',start_tstamp,' and BitDepth is null') into @s;
   prepare stmt from @s; execute stmt; deallocate prepare stmt;
   commit;
   
   set bigintCounter = start_tstamp;
   while bigintCounter<=stop_tstamp do
      /* fill in NULL values that might be scattered in result set, by looking farther back in cached data than just the previous 1 second */
      update temp_RigState_results set HoleDepth=(select ifnull(floatvalue,intvalue) from temp_sqlth_data_1 where t_stamp<bigIntCounter order by t_stamp desc limit 1) where onesecond_epoch=bigIntCounter and HoleDepth is null; commit;
      update temp_RigState_results set Toolface=(select ifnull(floatvalue,intvalue) from temp_sqlth_data_2 where t_stamp<bigIntCounter order by t_stamp desc limit 1) where onesecond_epoch=bigIntCounter and Toolface is null; commit;
      update temp_RigState_results set QuillOscEnabled=(select ifnull(floatvalue,intvalue) from temp_sqlth_data_3 where t_stamp<bigIntCounter order by t_stamp desc limit 1) where onesecond_epoch=bigIntCounter and QuillOscEnabled is null; commit;
      update temp_RigState_results set BitDepth=(select ifnull(floatvalue,intvalue) from temp_sqlth_data_4 where t_stamp<bigIntCounter order by t_stamp desc limit 1) where onesecond_epoch=bigIntCounter and BitDepth is null; commit;
      
      set bigintCounter = bigintCounter + 1000;

   end while;

   select onesecond_epoch as start_tstamp,onesecond_epoch+1000 as end_tstamp,HoleDepth,BitDepth,Toolface,QuillOscEnabled from temp_RigState_results order by onesecond_epoch;
   
   /**** REMOVE FINAL SECTION WHICH AGGLOMERATES RESULTS, COMMENTING OUT IN CASE NEEDED LATER ... ****/
   /* final requirement - using results, build a simple table with from tstamp and to tstamp */
/*   set bigintCounter = start_tstamp;

   select HoleDepth,Toolface,QuillOscEnabled,BitDepth into current_HoleDepth,current_Toolface,current_QO,current_BitDepth from temp_RigState_results where onesecond_epoch = bigIntCounter;
   create temporary table temp_final_results(start_tstamp bigint,end_tstamp bigint,HoleDepth float,Toolface float,QuillOscEnabled int,BitDepth float);
   insert into temp_final_results select start_tstamp,null as end_tstamp,current_HoleDepth as HoleDepth,current_Toolface as Toolface,current_QO as QuillOscEnabled,current_BitDepth as BitDepth from temp_RigState_results where onesecond_epoch=bigIntCounter;
   while bigIntCounter<=stop_tstamp do
      select HoleDepth,Toolface,QuillOscEnabled,BitDepth into next_HoleDepth,next_Toolface,next_QO,next_BitDepth from temp_RigState_results where onesecond_epoch = bigIntCounter;
      if (current_HoleDepth!=next_HoleDepth) OR (current_Toolface!=next_Toolface) OR (current_QO!=next_QO) OR (current_BitDepth!=next_BitDepth) then
         update temp_final_results set end_tstamp=bigIntCounter where end_tstamp is null;
         insert into temp_final_results select bigIntCounter,null,next_HoleDepth,next_Toolface,next_QO,next_BitDepth;
      end if;
      set current_HoleDepth=next_HoleDepth;
      set current_Toolface=next_Toolface;
      set current_QO=next_QO;
      set current_BitDepth=next_BitDepth;
      
      set bigintCounter = bigintCounter + 1000;
   end while;
   update temp_final_results set end_tstamp=stop_tstamp where end_tstamp is null;
   select * from temp_final_results order by start_tstamp;
*/
   /*** END COMMENT OUT OF AGGLOMERATION SECTION ***/
   
   drop temporary table if exists `temp_sqlth_data_1`;
   drop temporary table if exists `temp_sqlth_data_2`;
   drop temporary table if exists `temp_sqlth_data_3`;
	drop temporary table if exists `temp_sqlth_data_4`;
   drop temporary table if exists `temp_final_results`;

END