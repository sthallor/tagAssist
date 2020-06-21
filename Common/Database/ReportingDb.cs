using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Database.Context;
using Common.Models.Reporting;
using log4net;

namespace Common.Database
{
    public static class ReportingDb
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static List<RigRemarks> GetRigRemarks(string rig)
        {
            using (var dbContext = new ReportingDbContext())
            {
                var sql = $@"SELECT * FROM (
select top 5 COALESCE(wdpr.ActivityCodeDescriptionOverride, rt.LongName, '') as RemarkType, COALESCE(wdpr.Remark, '') as Remark, wdpr.EffectiveDate
from ignrpt_BLACKGOLD.BlackGold_PROD.dbo.WellDrillPeriod wdp 
join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.WellDrillPeriodRemark wdpr on wdp.WellDrillPeriodID = wdpr.Tour_WellDrillPeriodID
join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.rRemarkType rt on wdpr.RemarkType_rRemarkTypeID = rt.rRemarkTypeID
where wdp.ActiveInd=1 and wdpr.ActiveInd=1 and wdp.ReportedRigName = '{rig}'
order by wdpr.EffectiveDate desc) sq ORDER BY EffectiveDate
 ";
                return dbContext.Database.SqlQuery<RigRemarks>(sql).ToList();
            }
        }

        public static List<TagData> GetTagData()
        {
            using (var dbContext = new ReportingDbContext())
            {
                dbContext.Database.CommandTimeout = int.MaxValue;
                const string sql = @"
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

drop table if exists #rigTags
drop table if exists #historianData
drop table if exists #historianInfo
drop table if exists #TagsByRig
drop table if exists #RigsMovingPrevious48hrs
drop table if exists #edr
drop table if exists #wdp

select distinct ReportedRigName into #RigsMovingPrevious48hrs from ignrpt_BLACKGOLD.BlackGold_PROD.dbo.WellDrillPeriod where WellDrillPeriodID in (select Tour_WellDrillPeriodID from ignrpt_BLACKGOLD.BlackGold_PROD.dbo.WellDrillPeriodRemark where RemarkType_rRemarkTypeID in (select rRemarkTypeID from ignrpt_BLACKGOLD.BlackGold_PROD.dbo.rRemarkType where LongName like 'Skid%' or LongName like 'Move%' /*or LongName like 'Walk%'*/) and convert(datetime,EffectiveDate)>dateadd(day,-2,getdate()))

select d.division,d.nice_name as rig,te.id as tagid,te.tagpath,tags.display_name,te.retired,d.control_system,d.active,d.rig_status,d.EGN
  into #TagsByRig
  from sqlth_drv d
       inner join sqlth_scinfo sc on d.id = sc.drvid
       inner join sqlth_te te on sc.id = te.scid
       left outer join tags on te.tagpath=tags.tag_path
where te.retired is null
   and d.active='Y'
   and not(d.ace_os_prov_date is not null and te.tagpath like 'adr_pilot%') --leave out adr_pilot tags if the rig has been upgraded to a full RigOS
   and d.hist_prov_date is not null /* must have a historian installed */

																										 
select case when left(tagpath,6)='topdri' or tagpath like '%cj2m-cpu12-top drive%' then rig+'-TD' when left(tagpath,6)='softto' then rig+'-ST' when left(tagpath,6)='adr_pi' then rig+'-Edge' else rig end as rig
      ,tagid,control_system,division,'http://'+egn+':8088' as egn,rig_status
  into #RigTags
  from #TagsByRig
where rig not in ('Central')
   and Active='Y'
   and tagpath in (select tag_path from tags where display_name in ('top drive gear ratio','Top Drive Raw Signal','BLOCK_HEIGHT','HOOKLOAD','Soft Torque - Calculated Torque','quillspeedpv','pason_hookload'))

--select * from #RigTags where rig like 'T100%'

declare @ms_in_day bigint = 60 * 60 * 24 * 1000
declare @rightNowInGreenwich bigint = (@ms_in_day * datediff(day, '1970-01-01', getutcdate())) - datediff(millisecond, getutcdate(), cast(getutcdate() as date))
declare @480hoursAgoInGreenwich bigint = @rightNowInGreenwich - (@ms_in_day * 20)
select d.tagid,d.t_stamp,d.dataintegrity into #historiandata from sqlth_1_data d with (index = CCI_sqlth_1_data) inner join #RigTags rt on d.tagid=rt.tagid where d.t_stamp>=@480hoursAgoInGreenwich and d.t_stamp<@rightNowInGreenwich
delete from #historiandata where dataintegrity!=192 -- get rid of the poor quality data
create unique clustered index IX_tempHistorian on #historianData (tagid,t_stamp)
select case when left(tags.tagpath,6)='topdri' or tagpath like '%cj2m-cpu12-top drive%' then tags.rig+'-TD' when left(tags.tagpath,6)='softto' then tags.rig+'-ST' when left(tagpath,6)='adr_pi' then rig+'-Edge' else tags.rig end as rig
   ,dbo.[EnsignConvertToLocalTime](DATEADD(MILLISECOND, max(d.t_stamp) % 1000, DATEADD(SECOND, max(d.t_stamp) / 1000, '19700101')),-1*tz.UTCoffset,tz.usesDST) as lastHistorianTagInRigTimeZone
   ,datediff(second,dateadd(millisecond,max(d.t_stamp)%1000,dateadd(second,max(d.t_stamp)/1000,'19700101')),getutcdate())/60.000/60.000 as hoursSinceLastTag
  into #historianInfo
  from #historianData d
    inner join #TagsByRig tags on d.tagid=tags.tagid
    inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.equipment e on tags.rig=e.referencenum collate Latin1_General_CI_AS and e.source not in ('Nickles','IHS')
    inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.equipmentstatus es on e.equipmentid=es.equipment_equipmentid and es.activeind=1
    inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.rTimeZone tz on es.TimeZone_rTimezoneID=tz.rTimeZoneID
group by case when left(tags.tagpath,6)='topdri' or tagpath like '%cj2m-cpu12-top drive%' then tags.rig+'-TD' when left(tags.tagpath,6)='softto' then tags.rig+'-ST' when left(tagpath,6)='adr_pi' then rig+'-Edge' else tags.rig end
        ,tz.UTCoffset,tz.usesDST

select *
  into #wdp
  from (
select ReportedRigName,EndDateTime,Source,dense_rank() over (partition by ReportedRigName order by StartDateTime desc,Well_WellID desc) as rank
  from ignrpt_BLACKGOLD.BlackGold_PROD.dbo.WellDrillPeriod
where ActiveInd=1
       ) x
where x.rank=1

select wdp.ReportedRigName,max(edr.MeasurementDateTime) as LastEdrOnRig
  into #EDR
  from ignrpt_BLACKGOLD.BlackGold_PROD.dbo.WellDrillPeriod wdp
       inner join ignrpt_BLACKGOLD.BlackGold_PROD.dbo.WellEDR edr on wdp.Well_WellID=edr.Well_WellID
where wdp.ActiveInd=1
group by wdp.ReportedRigName


insert into @TableVarPrinting
select rig.rig
       ,rig.control_system
       ,rig.division
       ,rig.egn
       ,historian.lastHistorianTagInRigTimeZone
       ,round(historian.hoursSinceLastTag,2)
       ,convert(datetime,edr.LastEdrOnRig) as LastEDR
       ,convert(datetime,wdp.EndDateTime) as LastTour
       ,case when wdp.Source in ('NOV XML','NOV 2.0','RMS') then 'NOV' when wdp.Source like 'Pason%' or wdp.Source='ETS2.2 Conversion' then 'Pason' when wdp.Source='myWells' then 'CanRig' else 'RigManager.com' end as TourSource
       ,round(datediff(second,edr.LastEdrOnRig,getutcdate())/60.000/60.000,2) --all EDR records have a built-in UTC offset so compare to getutcdate() and should always get an accurate datediff
       ,round(datediff(second,convert(datetime,wdp.EndDateTime),getdate())/60.000/60.000,2)
       ,rig.rig_status
       ,jira.ticket_number
       ,jira.ticket_hyperlink
       ,(select case when replace(replace(replace(replace(rig.rig,'-',''),'Edge',''),'TD',''),'ST','') in (select ReportedRigName collate Latin1_General_CI_AI from #RigsMovingPrevious48hrs) then 'Y' else 'N' end) as RigMovingPrevious48
  from (select distinct rig,control_system,division,egn,rig_status from #RigTags where rig!='161-TD' /* TD has been replaced by Edge Controls on 161 */) rig
        left outer join OPENQUERY(IGNRPT_BLACKGOLD,'SELECT * FROM BLACKGOLD_IGNITION.IgnitionEnterprise.dbo.rig_issuetracking_tickets') jira on replace(replace(replace(replace(rig.rig,'-',''),'Edge',''),'TD',''),'ST','')=jira.rig and jira.isactive=1 --TO DO: move issue tracking table to IGNRPTing database to avoid this strange double-linked-server hop
        left outer join #wdp wdp on wdp.ReportedRigName collate Latin1_General_CI_AS=replace(replace(replace(replace(rig.rig,'-',''),'Edge',''),'TD',''),'ST','')
        left outer join #edr edr on edr.ReportedRigName collate Latin1_General_CI_AS=replace(replace(replace(replace(rig.rig,'-',''),'Edge',''),'TD',''),'ST','')
        outer apply (select lastHistorianTagInRigTimeZone,hoursSinceLastTag from #historianInfo where rig=rig.rig collate Latin1_General_CI_AS) historian

select b.nice_name as Rig, a.rig as Device, a.control_system as ControlSystem, a.Division,
 cast(HoursSinceLastTag as decimal(10,2)) as HoursSinceLastTag,
 latest_tour_source as TourSource,
 cast(HoursSinceLastEDR as decimal(10,2)) as HoursSinceLastEDR,
 cast(HoursSinceLastTour as decimal(10,2)) as HoursSinceLastTour
 from @TableVarPrinting a
join sqlth_drv b on replace(replace(a.egn,'http://',''),':8088','') = b.egn collate database_default

drop table if exists #rigTags
drop table if exists #historianData
drop table if exists #historianInfo
drop table if exists #TagsByRig
drop table if exists #RigsMovingPrevious48hrs
drop table if exists #edr
drop table if exists #wdp";
                var tagData = dbContext.Database.SqlQuery<TagData>(sql).ToList();
                return tagData;
            }
        }

        public static List<EgnServer> GetEgnServers()
        {
            return GetEgnServers(@"
select Division, nice_name as RigNumber, control_system as ControlSystem, EGN as Server
from sqlth_drv 
where division <> 'ENT' and active = 'Y' and rig_status = 'Active' and egn is not null and hist_prov_date is not null
order by division, nice_name");
        }
        public static List<EgnServer> GetAllEgnServers()
        {
            return GetEgnServers(@"
select Division, nice_name as RigNumber, control_system as ControlSystem, EGN as Server
from sqlth_drv 
where division <> 'ENT' and egn is not null and hist_prov_date is not null
order by division, nice_name");
        }
        public static List<EgnServer> GetAllEgn()
        {
            return GetEgnServers(@"
select Division, nice_name as RigNumber, control_system as ControlSystem, EGN as Server
from sqlth_drv 
where division <> 'ENT'
order by division, nice_name");
        }

        public static List<EgnServer> GetStackedEgnServers()
        {
            return GetEgnServers(@"
select Division, nice_name as RigNumber, control_system as ControlSystem, EGN as Server
from sqlth_drv 
where rig_status = 'Stacked' and egn is not null and hist_prov_date is not null
order by division, nice_name");
        }

        public static List<EgnServer> GetEgnServers(string sql)
        {
            using (var dbContext = new ReportingDbContext())
            {
                dbContext.Database.CommandTimeout = int.MaxValue;
                var egnServers = dbContext.Database.SqlQuery<EgnServer>(sql).ToList();

                var rigsOnly = ConfigurationManager.AppSettings["RigsOnly"].Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList();
                var rigsExcept = ConfigurationManager.AppSettings["RigsExcept"].Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList();
                if (rigsOnly.Any())
                {
                    Log.Info($"Only processing rigs: {ConfigurationManager.AppSettings["RigsOnly"]}");
                    egnServers = egnServers.Where(x => rigsOnly.Contains(x.RigNumber)).ToList();
                }
                if (rigsExcept.Any())
                {
                    Log.Info($"Processing all rigs except: {ConfigurationManager.AppSettings["RigsOnly"]}");
                    egnServers = egnServers.Where(x => !rigsExcept.Contains(x.RigNumber)).ToList();
                }
                return egnServers;
            }
        }
        public static List<string> GetMissingEdrTags()
        {
            using (var dbContext = new ReportingDbContext())
            {
                const string sql = @"select 'insert into sqlth_te values(''edr_data/hole_depth'',' + ltrim(str(b.id)) + ',1,3,1477685085000,null)' as query
from sqlth_drv a join sqlth_scinfo b on a.id = b.drvid
where a.active='Y' and b.id not in (select scid from sqlth_te where tagpath='edr_data/hole_depth')
union
select 'insert into sqlth_te values(''edr_data/bit_depth'',' + ltrim(str(b.id)) + ',1,3,1477685085000,null)' as query
from sqlth_drv a join sqlth_scinfo b on a.id = b.drvid
where a.active='Y' and b.id not in (select scid from sqlth_te where tagpath='edr_data/bit_depth')
";
                return dbContext.Database.SqlQuery<string>(sql).ToList();
            }
        }

        public static DateTime GetLastTag()
        {
            using (var dbContext = new ReportingDbContext())
            {
                const string sql = "select top 1 t_stamp from sqlth_1_data order by t_stamp desc";
                var reportingLastTag = dbContext.Database.SqlQuery<long>(sql).FirstOrDefault();
                return Utility.UnixTimeStampToDateTime(reportingLastTag);
            }
        }

        public static List<EgnServer> GetEgnServersSql(string sql)
        {
            using (var dbContext = new ReportingDbContext())
            {
                return dbContext.Database.SqlQuery<EgnServer>(sql).ToList();
            }
        }

        public static string GetRigRemarksHtml(string rig)
        {
            var rigRemarks = GetRigRemarks(rig);
            var htmlData = "";
            foreach (var remark in rigRemarks)
            {
                htmlData += $"<tr><td>{remark.RemarkType}</td><td>{remark.EffectiveDate:g}</td><td style=\"text - align:left\">{remark.Remark}</td></tr>";
            }
            var htmlTable = $@"<html><head><style type = ""text/css"">
    table, td, th{{
      border: 1px solid black; 
         padding: 5px;
         border-collapse: collapse;
         font-family: calibri;
                                font-size: 14px;
         text-align: center;
    }} 
        h3 {{
         font-family: calibri;
    }} 
    </style></head><body>
<H3>Most Recent Tour Remarks</H3><table border = 1><tr><th>Code</th><th>Remark Time</th><th style=&quot;text-align:left&quot;>Remark</th></tr>{htmlData}</table></body></html>";
            return htmlTable;
        }

        public static string GetRigReportHtml(string rigs)
        {
            Log.Info($"Received paramter {rigs}");
            rigs = rigs.Replace(",", "','");
            Log.Info($"{rigs}");
            using (var dbContext = new ReportingDbContext())
            {
                var sql = File.ReadAllText(@"C:\Program Files\IgorEnterprise\Scripts\ThriceDailyByRig.sql").Replace("<RIGHERE>", rigs);
                //Log.Debug(sql);
                var s = dbContext.Database.SqlQuery<Thingy>(sql).ToList();
                return s.FirstOrDefault().abc;
            }
        }
    }

    internal class Thingy
    {
        public string abc { get; set; }
    }
}