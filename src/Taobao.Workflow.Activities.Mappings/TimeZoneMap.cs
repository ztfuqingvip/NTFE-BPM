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
    public class TimeZoneMap : ClassMap<TimeZone>
    {
        public TimeZoneMap()
        {
            Table("NTFE_TimeZone");
            Id(m => m.ID);
            Map(m => m.Name).Length(50);
            Map(m => m.Description).Length(500);
            Map(m => m.CreateTime);

            HasMany<WorkingDay>(Reveal.Member<TimeZone>("_days"))
                .Cascade.All()
                .KeyColumn("TimeZoneId")
                .LazyLoad();
        }
    }

    public class WorkingDayMap : ClassMap<WorkingDay>
    {
        public WorkingDayMap()
        {
            Table("NTFE_TimeZone_WorkingDay");
            Id(m => m.ID);
            Map(m => m.Date);
            Map(m => m.DayOfWeek).CustomType<DayOfWeek?>();
            Map(m => m.Description);
            Map(Reveal.Member<WorkingDay>("_hours")).Column("WorkingHours");
        }
    }
}
