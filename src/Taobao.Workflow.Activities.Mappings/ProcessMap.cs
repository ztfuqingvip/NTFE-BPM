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
using FluentNHibernate;

namespace Taobao.Workflow.Activities.Mappings
{
    public class ProcessMap : ClassMap<Process>
    {
        public ProcessMap()
        {
            Table("NTFE_Process");
            //HACK:允许由应用设置，增加应用友好
            Id(m => m.ID).GeneratedBy.Assigned();

            Map(m => m.CreateTime);
            Map(m => m.FinishTime).Nullable();
            Map(m => m.Priority);
            Map(m => m.Status).CustomType<ProcessStatus>();
            Map(m => m.Title).Length(500);
            Map(m => m.ParentProcessId);

            //调度标识字段
            Map(Reveal.Member<Process>("_chargingBy")).Column("ChargingBy").Length(50);
            //HACK:由于Component的lazyload在当前NH版本下无效，改为单独维护WorkflowData字段
            //Component<Process.InternalWorkflowInstance>(Reveal.Member<Process>("Instance")
            //    , m => m.Map(x => x.Serialized).Column("WorkflowData"))
            //    .LazyLoad();

            References(m => m.Originator).Column("OriginatorId").LazyLoad();
            References(m => m.ProcessType).Column("ProcessTypeId").LazyLoad();

            HasMany<ProcessDataField>(Reveal.Member<Process>("_dataFields"))
                .Cascade.SaveUpdate()
                .LazyLoad()
                .KeyColumn("ProcessId");
        }
    }

    public class ProcessDataFieldMap : ClassMap<ProcessDataField>
    {
        public ProcessDataFieldMap()
        {
            Table("NTFE_ProcessDataField");
            Id(m => m.ID);
            Map(m => m.Name).Length(50);
            Map(m => m.Value).Length(500);
        }
    }
}
