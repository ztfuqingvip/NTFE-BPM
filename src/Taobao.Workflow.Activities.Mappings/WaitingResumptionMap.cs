/*
    Copyright (C) 2012  Alibaba

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentNHibernate.Mapping;
using Taobao.Workflow.Activities.Hosting;
using FluentNHibernate;

namespace Taobao.Workflow.Activities.Mappings
{
    public class WaitingResumptionMap : ClassMap<WaitingResumption>
    {
        public WaitingResumptionMap()
        {
            Table("NTFE_WaitingResumption");
            Id(m => m.ID);
            Map(m => m.CreateTime);
            Map(m => m.Priority);
            Map(m => m.IsValid);
            Map(m => m.IsExecuted);
            Map(m => m.IsError);
            Map(m => m.ChargingBy).Nullable();
            Map(m => m.At).Nullable();

            References(m => m.Process).Column("ProcessId").Fetch.Join();
            DiscriminateSubClassesOnColumn("ResumptionType").Length(255);
        }
    }
    public class ProcessStartResumptionMap : SubclassMap<ProcessStartResumption>
    {
        public ProcessStartResumptionMap()
        {
            Table("NTFE_WaitingResumption");
            DiscriminatorValue("start");
        }
    }
    public class BookmakrResumptionMap : SubclassMap<BookmarkResumption>
    {
        public BookmakrResumptionMap()
        {
            Table("NTFE_WaitingResumption");
            Map(m => m.ActivityName).Length(50);
            Map(m => m.BookmarkName).Length(50);
            Map(m => m.Value).Length(50);
            DiscriminatorValue("bookmark");
        }
    }
    public class ErrorResumptionMap : SubclassMap<ErrorResumption>
    {
        public ErrorResumptionMap()
        {
            Table("NTFE_WaitingResumption");
            Map(m => m.ErrorNodeIndex);
            DiscriminatorValue("error");
        }
    }
    public class WorkItemCreateResumptionMap : SubclassMap<WorkItemCreateResumption>
    {
        public WorkItemCreateResumptionMap()
        {
            Table("NTFE_WaitingResumption");
            References(m => m.HumanActivityInstance).Column("HumanActivityInstanceId").Fetch.Join();
            DiscriminatorValue("w_create");
        }
    }
    public class SubProcessCreateResumptionMap : SubclassMap<SubProcessCreateResumption>
    {
        public SubProcessCreateResumptionMap()
        {
            Table("NTFE_WaitingResumption");
            References(m => m.SubProcessActivityInstance).Column("SubProcessActivityInstanceId").Fetch.Join();
            DiscriminatorValue("sub_create");
        }
    }
    public class SubProcessCompleteResumptionMap : SubclassMap<SubProcessCompleteResumption>
    {
        public SubProcessCompleteResumptionMap()
        {
            Table("NTFE_WaitingResumption");
            References(m => m.SubProcess).Column("SubProcessId").Fetch.Join().Cascade.None();
            DiscriminatorValue("sub_complete");
        }
    }
    public class ActivityInstanceCancelResumptionMap : SubclassMap<ActivityInstanceCancelResumption>
    {
        public ActivityInstanceCancelResumptionMap()
        {
            Table("NTFE_WaitingResumption");
            References(m => m.ActivityInstance).Column("ActivityInstanceId").Fetch.Join();
            DiscriminatorValue("a_cancel");
        }
    }
    public class HumanEscalationResumptionMap : SubclassMap<HumanEscalationResumption>
    {
        public HumanEscalationResumptionMap()
        {
            Table("NTFE_WaitingResumption");
            References(m => m.HumanActivityInstance).Column("HumanActivityInstanceId").Fetch.Join();
            DiscriminatorValue("escalation");
        }
    }
}