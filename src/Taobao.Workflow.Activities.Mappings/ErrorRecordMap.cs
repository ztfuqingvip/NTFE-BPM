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
using FluentNHibernate;
using FluentNHibernate.Mapping;
using Taobao.Workflow.Activities.Hosting;

namespace Taobao.Workflow.Activities.Mappings
{
    public class ErrorRecordMap : ClassMap<ErrorRecord>
    {
        public ErrorRecordMap()
        {
            Table("NTFE_ErrorRecord");
            Id(m => m.ID);
            Map(m => m.CreateTime);
            Map(m => m.Reason);
            Map(Reveal.Member<ErrorRecord>("_isDeleted")).Column("IsDeleted");

            References(m => m.Process).Column("ProcessId").LazyLoad().Cascade.None();
            DiscriminateSubClassesOnColumn("ErrorType").Length(50);
        }
    }
    public class FaultBookmarkRecordMap : SubclassMap<FaultBookmarkRecord>
    {
        public FaultBookmarkRecordMap()
        {
            Table("NTFE_ErrorRecord");
            Map(m => m.BookmarkName).Length(50);
            Map(m => m.ActivityName).Length(50);
            DiscriminatorValue("bookmark");
        }
    }
    public class FaultResumptionRecordMap : SubclassMap<FaultResumptionRecord>
    {
        public FaultResumptionRecordMap()
        {
            Table("NTFE_ErrorRecord");
            Map(m => m.ResumptionId);
            DiscriminatorValue("resumption");
        }
    }
}
