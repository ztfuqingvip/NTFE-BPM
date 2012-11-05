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
    public class AgentMap : ClassMap<Agent>
    {
        public AgentMap()
        {
            Table("NTFE_Agent");
            Id(m => m.ID);
            Map(m => m.CreateTime);
            Map(m => m.BeginTime);
            Map(m => m.EndTime);
            Map(m => m.Range).CustomType<ActingRange>();
            Map(Reveal.Member<Agent>("_enable")).Column("Enable");

            References(m => m.ActAs).Column("ActAsUserId").LazyLoad();
            References(m => m.User).Column("UserId").LazyLoad();

            HasMany<ProcessActingItem>(Reveal.Member<Agent>("_actingItems"))
                .KeyColumn("AgentId")
                .Cascade.SaveUpdate()
                .LazyLoad();
        }
    }

    public class ProcessActingItemMap : ClassMap<ProcessActingItem>
    {
        public ProcessActingItemMap()
        {
            Table("NTFE_Agent_ProcessActingItem");
            Id(m => m.ID);
            Map(m => m.ProcessTypeName);
        }
    }
}
