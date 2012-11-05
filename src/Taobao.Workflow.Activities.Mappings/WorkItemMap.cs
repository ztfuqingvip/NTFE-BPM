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
using Taobao.Workflow.Activities.Statements;

namespace Taobao.Workflow.Activities.Mappings
{
    public class WorkItemMap : ClassMap<WorkItem>
    {
        public WorkItemMap()
        {
            Table("NTFE_WorkItem");
            Id(m => m.ID);

            Map(m => m.ArrivedTime);
            Map(m => m.CreateTime);
            Map(m => m.FinishTime).Nullable();
            Map(m => m.Result).Length(255);
            Map(m => m.Status).CustomType<WorkItemStatus>();

            //冗余字段
            Map(m => m.ActivityName).Length(50);
            Map(Reveal.Member<WorkItem>("_processTypeName")).Column("ProcessTypeName");

            References(m => m.OriginalActioner).Column("OriginalActionerId").LazyLoad();
            References(m => m.Actioner).Column("ActionerId").LazyLoad();
            //任务总是和流程同时被使用
            References(m => m.Process).Column("ProcessId").Fetch.Join();
            References(m => m.ActivityInstance).Column("HumanActivityInstanceId").LazyLoad();
        }
    }
}
