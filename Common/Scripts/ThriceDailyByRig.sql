declare @xml1 NVARCHAR(MAX)
declare @xml2 NVARCHAR(MAX)
declare @xml3 NVARCHAR(MAX)
declare @xml4 NVARCHAR(MAX)
declare @xml5 NVARCHAR(MAX)
declare @bodyHTML NVARCHAR(MAX)

declare @TableVarPrinting table(
    rig varchar(255),
    control_system varchar(20),
    division varchar(10),
    egn varchar(40),
    latest_tag_datetime varchar(50),
    HoursSinceLastTag float,
    latest_EDR_datetime varchar(50),
    latest_tourstart_datetime varchar(50),
    latest_tour_source varchar(50),
    HoursSinceLastEDR float,
    HoursSinceLastTour float,
    rig_status varchar(20),
    ticket_number varchar(30),
    ticket_hyperlink varchar(500),
    RigMovingPrevious48 varchar(1)
	);

if object_id('tempdb..#rigTags') is not null drop table #rigTags
if object_id('tempdb..#historianData') is not null drop table #historianData
if object_id('tempdb..#historianInfo') is not null drop table #historianInfo
if object_id('tempdb..#TagsByRig') is not null drop table #TagsByRig
if object_id('tempdb..#RigsMovingPrevious48hrs') is not null drop table #RigsMovingPrevious48hrs

select distinct ReportedRigName into #RigsMovingPrevious48hrs from ignrpt_BLACKGOLD.BlackGold_PROD.dbo.WellDrillPeriod where WellDrillPeriodID in (select Tour_WellDrillPeriodID from ignrpt_BLACKGOLD.BlackGold_PROD.dbo.WellDrillPeriodRemark where RemarkType_rRemarkTypeID in (select rRemarkTypeID from ignrpt_BLACKGOLD.BlackGold_PROD.dbo.rRemarkType where LongName like 'Skid%' or LongName like 'Move%' /*or LongName like 'Walk%'*/) and convert(datetime,EffectiveDate)>dateadd(day,-2,getdate()))

select d.division,d.nice_name as rig,te.id as tagid,te.tagpath,tags.display_name,te.retired,d.control_system,d.active,d.rig_status,d.EGN
  into #TagsByRig
  from sqlth_drv d
       inner join sqlth_scinfo sc on d.id = sc.drvid
       inner join sqlth_te te on sc.id = te.scid
       left outer join tags on te.tagpath=tags.tag_path
 where te.retired is null
   and d.active='Y' and d.nice_name in('<RIGHERE>')
   and not(d.ace_os_prov_date is not null and te.tagpath like 'adr_pilot%') --leave out adr_pilot tags if the rig has been upgraded to a full RigOS

select case when left(tagpath,6)='topdri' or left(tagpath,20)='cj2m-cpu12-top drive' then rig+'-TD' when left(tagpath,6)='softto' then rig+'-ST' when left(tagpath,6)='adr_pi' then rig+'-Edge' else rig end as rig
      ,tagid,control_system,division,'http://'+egn+':8088' as egn,rig_status
  into #RigTags
  from #TagsByRig
 where rig not in ('Central')
   and Active='Y'
   and tagpath in (select tag_path from tags where display_name in ('top drive gear ratio','Top Drive Raw Signal','BLOCK_HEIGHT','HOOKLOAD','Soft Torque - Calculated Torque','quillspeedpv','pason_hookload'))

declare @ms_in_day bigint = 60 * 60 * 24 * 1000
declare @rightNowInGreenwich bigint = (@ms_in_day * datediff(day, '1970-01-01', getutcdate())) - datediff(millisecond, getutcdate(), cast(getutcdate() as date))
declare @480hoursAgoInGreenwich bigint = @rightNowInGreenwich - (@ms_in_day * 20)
select d.tagid,d.t_stamp,d.dataintegrity into #historiandata from sqlth_1_data d with (index = CCI_sqlth_1_data) inner join #RigTags rt on d.tagid=rt.tagid where d.t_stamp>=@480hoursAgoInGreenwich and d.t_stamp<@rightNowInGreenwich
delete from #historiandata where dataintegrity!=192 -- get rid of the poor quality data
create unique clustered index IX_tempHistorian on #historianData (tagid,t_stamp)
select case when left(tags.tagpath,6)='topdri' or left(tagpath,20)='cj2m-cpu12-top drive' then tags.rig+'-TD' when left(tags.tagpath,6)='softto' then tags.rig+'-ST' when left(tagpath,6)='adr_pi' then rig+'-Edge' else tags.rig end as rig
   ,dbo.[EnsignConvertToLocalTime](DATEADD(MILLISECOND, max(d.t_stamp) % 1000, DATEADD(SECOND, max(d.t_stamp) / 1000, '19700101')),-1*tz.UTCoffset,tz.usesDST) as lastHistorianTagInRigTimeZone
   ,datediff(second,dateadd(millisecond,max(d.t_stamp)%1000,dateadd(second,max(d.t_stamp)/1000,'19700101')),getutcdate())/60.000/60.000 as hoursSinceLastTag
  into #historianInfo
  from #historianData d
    inner join #TagsByRig tags on d.tagid=tags.tagid
    inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.equipment e on tags.rig=e.referencenum collate Latin1_General_CI_AS and e.source not in ('Nickles','IHS')
    inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.equipmentstatus es on e.equipmentid=es.equipment_equipmentid and es.activeind=1
    inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.rTimeZone tz on es.TimeZone_rTimezoneID=tz.rTimeZoneID
group by case when left(tags.tagpath,6)='topdri' or left(tagpath,20)='cj2m-cpu12-top drive' then tags.rig+'-TD' when left(tags.tagpath,6)='softto' then tags.rig+'-ST' when left(tagpath,6)='adr_pi' then rig+'-Edge' else tags.rig end
        ,tz.UTCoffset,tz.usesDST

--select * from #historianInfo

/*** Grab Modbus Data ***/
drop table if exists #customer_portal_rigs
select drv.nice_name
  into #customer_portal_rigs
  from sqlth_drv drv
       cross apply (select top 1 Well_WellID from IGNRPT_BLACKGOLD.BlackGold_PROD.dbo.WellDrillReport where ActiveInd=1 and ReportedRigNum=drv.nice_name collate Latin1_General_CI_AI order by StartDate desc) wdp
       inner join IGNRPT_BLACKGOLD.BlackGold_PROD.dbo.Well w on wdp.Well_WellID=w.WellID
       inner join IGNRPT_BLACKGOLD.BlackGold_PROD.dbo.BA_Alias baa on w.Operator_BusinessAssociateID=baa.BusinessAssociate_BusinessAssociateID and baa.AliasType_rAliasTypeID=16
       inner join IGNRPT_BLACKGOLD.BlackGold_PROD.dbo.BA_Preference bap on baa.Alias_BusinessAssociateID=bap.BusinessAssociate_BusinessAssociateID and bap.PreferenceType_rBAPrefTypeID=1
 where drv.active='Y'
drop table if exists #modbus_rigs
drop table if exists #modbus
select nice_name
  into #modbus_rigs
  from sqlth_drv d
       inner join sqlth_scinfo sc on d.id=sc.drvid
 where sc.id in (select distinct scid from sqlth_te where tagpath like '%pason_edr_iomap%' or tagpath like 'pason/%')
   and d.active='Y' and d.nice_name in ('<RIGHERE>')
create table #modbus (rig varchar(10),LastDate datetime,epoch bigint)
declare @30daysAgo bigint = convert(bigint,DATEDIFF(s, '1970-01-01', GETUTCDATE()))*1000 - convert(bigint,30)*24*60*60*1000
while exists (select 1 from #modbus_rigs)
begin
   declare @rig varchar(10)
   select top 1 @rig=nice_name from #modbus_rigs order by nice_name asc
   declare @tagid int
   select @tagid=te.id
     from sqlth_te te
          inner join sqlth_scinfo sc on te.scid=sc.id
          inner join sqlth_drv drv on sc.drvid=drv.id
    where (tagpath like '%pason_edr_iomap%' or tagpath like 'pason/%') and tagpath like '%rotary_rpm%' and retired is null
      and drv.nice_name=@rig
      and drv.active='Y' and drv.nice_name in ('<RIGHERE>')
   declare @last_tstamp bigint
   select top 1 @last_tstamp=t_stamp from sqlth_1_data with (nolock) where tagid=@tagid and t_stamp>@30daysAgo order by t_stamp desc
   insert into #modbus select @rig,dbo.fnEpochToRigDate(@last_tstamp,@rig) as LastDate,@last_tstamp
   delete from #modbus_rigs where nice_name=@rig
end
declare @xml89 varchar(max)
set @xml89 = CAST(( select case when cpr.nice_name is not null then '<span style="color:red;">'+rig+'</span>' else rig end as 'td','',LastDate as 'td','',case when cpr.nice_name is not null then '<span style="color:red;">Customer Portal Rig</span>' else '' end as 'td' from #modbus m left outer join #customer_portal_rigs cpr on m.rig=cpr.nice_name collate Latin1_General_CI_AI
 where (convert(bigint,DATEDIFF(s, '1970-01-01', GETUTCDATE()))*1000) - epoch > (1000*60*60*5) /* older than 5 hours */ order by rig
FOR XML PATH('tr'), ELEMENTS ) AS NVARCHAR(MAX))

insert into @TableVarPrinting
 select rig.rig
       ,rig.control_system
       ,rig.division
       ,rig.egn
       ,historian.lastHistorianTagInRigTimeZone
       ,round(historian.hoursSinceLastTag,2)
       ,convert(datetime,max(edr.maxEDRbyWell)) as LastEDR
       ,convert(datetime,max(wdpmostRecent100.EndDateTime)) as LastTour
       ,max(case when wdpmostRecent100.Source in ('NOV XML','NOV 2.0','RMS') then 'NOV' when wdpmostRecent100.Source like 'Pason%' or wdpmostRecent100.Source='ETS2.2 Conversion' then 'Pason' when wdpmostRecent100.Source='myWells' then 'CanRig' else 'RigManager.com' end) as TourSource  --cheated a bit and used a max here so we find only one source value.  To do this proper should grab source from the tour with the max end date
       ,round(datediff(second,max(edr.maxEDRbyWell),getutcdate())/60.000/60.000,2) --all EDR records have a built-in UTC offset so compare to getutcdate() and should always get an accurate datediff
       ,round(datediff(second,convert(datetime,max(wdpmostRecent100.EndDateTime)),getdate())/60.000/60.000,2)
       ,rig.rig_status
       ,jira.ticket_number
       ,jira.ticket_hyperlink
       ,(select case when replace(replace(replace(replace(rig.rig,'-',''),'Edge',''),'TD',''),'ST','') in (select ReportedRigName collate Latin1_General_CI_AI from #RigsMovingPrevious48hrs) then 'Y' else 'N' end) as RigMovingPrevious48
  from (select distinct rig,control_system,division,egn,rig_status from #RigTags where rig!='161-TD' /* TD has been replaced by Edge Controls on 161 */) rig
        left outer join OPENQUERY(IGNRPT_BLACKGOLD,'SELECT * FROM BLACKGOLD_IGNITION.IgnitionEnterprise.dbo.rig_issuetracking_tickets') jira on replace(replace(replace(replace(rig.rig,'-',''),'Edge',''),'TD',''),'ST','')=jira.rig and jira.isactive=1 --TO DO: move issue tracking table to IGNRPTing database to avoid this strange double-linked-server hop
        outer apply (select distinct Well_WellID,EndDateTime,ReportedRigName,Source from (select Well_WellID,EndDateTime,ReportedRigName,Source,dense_rank() over (partition by ReportedRigName order by StartDateTime desc) as rank from ignrpt_BLACKGOLD.BlackGold_PROD.dbo.WellDrillPeriod where activeind=1 and case when Source like '%TRIN%' then 'T'+right('000'+ReportedRigName,3) else ReportedRigName end collate Latin1_General_CI_AS=replace(replace(replace(replace(rig.rig,'-',''),'Edge',''),'TD',''),'ST','')) x where x.rank<=100) wdpmostRecent100
        outer apply (select max(measurementdatetime) as maxEDRbyWell from ignrpt_BLACKGOLD.BlackGold_PROD.dbo.WellEDR where well_wellid=wdpmostRecent100.Well_WellID) edr
        outer apply (select lastHistorianTagInRigTimeZone,hoursSinceLastTag from #historianInfo where rig=rig.rig collate Latin1_General_CI_AS) historian
group by rig.rig,historian.lastHistorianTagInRigTimeZone,historian.hoursSinceLastTag,rig.control_system,rig.division,rig.egn,rig.rig_status
        ,jira.ticket_number
        ,jira.ticket_hyperlink

set @xml1 = CAST(( select row_number() over (order by isnull(HoursSinceLastTag,9999) desc,p.rig asc) as 'td'
                ,'',p.[Division] as 'td'
                ,'', case when p.ticket_hyperlink is null then p.[Rig] else '<a href="' + p.ticket_hyperlink + '" title="JIRA: ' + p.ticket_number + '">' + p.rig + '</a>' end AS 'td'
                ,''
                ,'', isnull(convert(varchar(8),p.[HoursSinceLastTag]),'-')+case when isnull(p.[HoursSinceLastTag],6)>5 then '*' else '' end AS 'td'
                ,'', isnull(convert(varchar(8),p.[HoursSinceLastEDR]),'-') AS 'td'
                ,'', isnull(convert(varchar(8),p.[HoursSinceLastTour]),'-') AS 'td'
                ,'',p.[control_system] as 'td','',isnull(p.latest_tour_source,'-') as 'td','',p.[egn] as 'td','',isnull(p.[latest_tag_datetime],'>20 days') AS 'td','', isnull(p.latest_EDR_datetime,'-') AS 'td','', isnull(p.latest_tourstart_datetime,'-') as 'td','',isnull(tz.ShortName,'-') as 'td','',isnull(p.RigMovingPrevious48,'-') as 'td'
 from @TableVarPrinting p
      inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.equipment e on replace(replace(replace(replace(p.rig,'-',''),'Edge',''),'TD',''),'ST','')=e.referencenum collate Latin1_General_CI_AS and e.source not in ('Nickles','IHS')
      inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.equipmentstatus es on e.equipmentid=es.equipment_equipmentid and es.activeind=1
      inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.rTimeZone tz on es.TimeZone_rTimezoneID=tz.rTimeZoneID
where p.rig_status!='Stacked'
order by isnull(HoursSinceLastTag,9999) desc
FOR XML PATH('tr'), ELEMENTS ) AS NVARCHAR(MAX))

set @xml1=replace(replace(@xml1,'&lt;','<'),'&gt;','>')

/*** 23-May-2017 - repeat the header record after every 10 data records for readability */
declare @temp1 nvarchar(max)=@xml1
declare @i as int = 2 /* start at position 2, after the initial <tr> tag for the real header row */
declare @rowCounter as int =0
while (@i<len(@temp1))
begin
   if (substring(@temp1,@i,4)='<tr>')
   begin
      set @rowCounter=@rowCounter+1
	  if (@rowCounter%10=0) /* every 10th record */
	  begin
	     set @temp1=left(@temp1,@i-1)+'<tr bgcolor="#CCCCCC"><th> Row # </th> <th> Division </th> <th> Rig </th> <th> Hours Since Last Tag </th> <th> Hours Since Last EDR </th> <th> Hours Since Last Tour </th> <th> Control System </th> <th> Tour/EDR Source </th> <th> EGN </th> <th> Latest Tag </th> <th> Latest EDR Record </th> <th> Latest Toursheet </th> <th> Time Zone </th> <th> Moved in past 48hrs? </th> </tr>'+right(@temp1,len(@temp1)-@i+1)
		 set @i=@i+376 /* we've just lengthed @temp1, so bump @i forward to ignore the row we just added above */
	   end
   end
   set @i=@i+1
end
set @xml1=@temp1

/************* delinquent rig remarks *********************/
set @xml3 = cast((
select td=x.rig,'',td=x.shortname,'',td=convert(varchar(20),x.EffectiveDate,100),'',[td/@style]='text-align:left',td=x.remark from (
select distinct tvp.rig,isnull(wdpr.ActivityCodeDescriptionOverride,rt.LongName) as ShortName,wdpr.Remark,wdpr.EffectiveDate,dense_rank() over (partition by tvp.rig order by wdpr.EffectiveDate desc) as rank
  from (select * from @TableVarPrinting where rig_status!='Stacked') tvp
       inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.WellDrillPeriod wdp on tvp.rig=case when wdp.Source like '%Trin%' then 'T'+right('000'+wdp.ReportedRigName,3) else wdp.ReportedRigName end collate SQL_Latin1_General_CP1_CI_AS and wdp.ActiveInd=1
       inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.WellDrillPeriodRemark wdpr on wdp.WellDrillPeriodID=wdpr.Tour_WellDrillPeriodID and wdpr.ActiveInd=1 and wdpr.RemarkType_rRemarkTypeID!=1
       inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.rRemarkType rt on wdpr.RemarkType_rRemarkTypeID=rt.rRemarkTypeID
 where isnull(tvp.HoursSinceLastTag,6)>5 -- only rigs that haven't reported in the last 5 hours
 ) x where x.rank<=5 -- five most recent comments
 order by x.rig asc,x.EffectiveDate asc
 FOR XML PATH('tr'),elements) as nvarchar(max))
/**********************************************************/

/******************* Stacked Rigs *********************/
set @xml4 = CAST(( select row_number() over (order by isnull(HoursSinceLastTag,9999) desc,p.rig asc) as 'td','',p.[Division] as 'td','', p.[Rig] AS 'td',''
                ,'', isnull(convert(varchar(8),p.[HoursSinceLastTag]),'-') AS 'td'
                ,'', isnull(convert(varchar(8),p.[HoursSinceLastEDR]),'-') AS 'td'
                ,'', isnull(convert(varchar(8),p.[HoursSinceLastTour]),'-') AS 'td'
                ,'',p.[control_system] as 'td','',isnull(p.latest_tour_source,'-') as 'td','',p.[egn] as 'td','',isnull(p.[latest_tag_datetime],'>10 days') AS 'td','', isnull(p.latest_EDR_datetime,'-') AS 'td','', isnull(p.latest_tourstart_datetime,'-') as 'td','',isnull(tz.ShortName,'-') as 'td'
from @TableVarPrinting p
    inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.equipment e on p.rig=e.referencenum collate Latin1_General_CI_AS and e.source not in ('Nickles','IHS')
    inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.equipmentstatus es on e.equipmentid=es.equipment_equipmentid and es.activeind=1
    inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.rTimeZone tz on es.TimeZone_rTimezoneID=tz.rTimeZoneID
where p.rig_status='Stacked'
order by isnull(HoursSinceLastTag,9999) desc
FOR XML PATH('tr'), ELEMENTS ) AS NVARCHAR(MAX))
/**********************************************************************/

/* Build the HTML for the report */
/* first just set the style up */
SET @bodyHTML ='<html>
  <head>
    <meta charset = "UTF-8" />
    <style type = "text/css">
    table, td, th{
      border: 1px solid black; 
         padding: 5px;
         border-collapse: collapse;
         font-family: calibri;
		 font-size: 12px;
         text-align: center;
    } 
        h3 {
         font-family: calibri;
    } 
    </style>
  </head>
<body>'

/* Before moving on, include details on delinquent Pason Modbus tags */
if (@xml89 is not null)
begin
   set @bodyHTML = @bodyHTML + '<h3>Rigs not reporting Pason Modbus Data</h3><table border = 1><tr bgcolor="#CCCCCC"><th>Rig</th><th>Last Modbus Tag Date</th><th>Priority Customer?</th></tr>'+@xml89+'</table>'
end

/* Second - the main part of the report to show tag/tour/EDR history of each active rig */
if (@xml1 is not null)
begin
   set @bodyHTML = @bodyHTML + '<h3>Edge Historian Statistics @ ' + CONVERT(VARCHAR(19),getdate()) +  ' (MST)</h3><table border = 1><tr bgcolor="#CCCCCC"><th> Row # </th> <th> Division </th> <th> Rig </th> <th> Hours Since Last Tag </th> <th> Hours Since Last EDR </th> <th> Hours Since Last Tour </th> <th> Control System </th> <th> Tour/EDR Source </th> <th> EGN </th> <th> Latest Tag </th> <th> Latest EDR Record </th> <th> Latest Toursheet </th> <th> Time Zone </th> <th> Moved in past 48hrs? </th> </tr>' + @xml1 + '</table>'
end

/* Third - of those rigs with delayed tag information, show most recent tour remarks */
if(@xml3 is not null)
begin
   set @bodyHTML = @bodyHTML + '<br><H3>*Most Recent Tour Remarks for Rigs with Delinquent Tags</H3><table border = 1><tr><th>Rig</th><th>Code</th><th>Remark Time</th><th style=&quot;text-align:left&quot;>Remark</th></tr>' + @xml3 + '</table>'
end

/* Fifth (last) - report on rigs currently marked as 'stacked' in the sqlth_drv table */
if(@xml4 is not null)
begin
   set @bodyHTML = @bodyHTML + '<h3>Stacked Rigs</h3><table border = 1><tr><th> Row # </th> <th> Division </th> <th> Rig </th> <th> Hours Since Last Tag </th> <th> Hours Since Last EDR </th> <th> Hours Since Last Tour </th> <th> Control System </th> <th> Tour/EDR Source </th> <th> EGN </th> <th> Latest Tag </th> <th> Latest EDR Record </th> <th> Latest Toursheet </th> <th> Time Zone </th> </tr>'+@xml4+'</table>'
end

/* close off the HTML, we're done */
set @bodyHTML = @bodyHTML + '</body></html>'

set @bodyHTML = replace(@bodyHTML,'&gt;','>')
set @bodyHTML = replace(@bodyHTML,'&lt;','<')
set @bodyHTML = replace(@bodyHTML,'&amp;','&')

--return @bodyHTML
select @bodyHTML as abc


if object_id('tempdb..#rigTags') is not null drop table #rigTags
if object_id('tempdb..#historianData') is not null drop table #historianData
if object_id('tempdb..#historianInfo') is not null drop table #historianInfo
if object_id('tempdb..#TagsByRig') is not null drop table #TagsByRig
