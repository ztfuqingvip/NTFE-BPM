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
using Taobao.Workflow.Activities.Statements;
using FluentNHibernate;

namespace Taobao.Workflow.Activities.Mappings
{
    public class ActivityInstanceBaseMap : ClassMap<ActivityInstanceBase>
    {
        public ActivityInstanceBaseMap()
        {
            Table("NTFE_ActivityInstance");
            Id(m => m.ID);
            Map(Reveal.Member<ActivityInstanceBase>("_workflowActivityInstanceId")).Column("WorkflowActivityInstanceId");
            Map(m => m.ActivityName).Length(50);
            Map(m => m.FlowNodeIndex);
            Map(m => m.ProcessId);
            Map(m => m.CreateTime);
            Map(m => m.FinishTime).Nullable();
            DiscriminateSubClassesOnColumn("ActivityInstanceType");
        }
    }
    public class HumanActivityInstanceMap : SubclassMap<HumanActivityInstance>
    {
        public HumanActivityInstanceMap()
        {
            Table("NTFE_ActivityInstance");
            Map(m => m.ReferredBookmarkName).Length(50);
            Map(Reveal.Member<HumanActivityInstance>("_actioners")).Column("Actioners");
            Map(Reveal.Member<HumanActivityInstance>("_alreadyActioners")).Column("AlreadyActioners");
            DiscriminatorValue("human");
        }
    }
    public class ServerActivityInstanceMap : SubclassMap<ServerActivityInstance>
    {
        public ServerActivityInstanceMap()
        {
            Table("NTFE_ActivityInstance");
            DiscriminatorValue("server");
        }
    }
    public class SubProcessInstanceMap : SubclassMap<SubProcessActivityInstance>
    {
        public SubProcessInstanceMap()
        {
            Table("NTFE_ActivityInstance");
            Map(m => m.SubProcessId).Nullable();
            Map(m => m.ReferredBookmarkName).Length(50);
            DiscriminatorValue("subproc");
        }
    }
}
