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
using CodeSharp.Core.Castles;
using NHibernate.Criterion;

namespace Taobao.Workflow.Activities.Repositories
{
    /// <summary>
    /// 时区信息仓储
    /// </summary>
    public class TimeZoneRepository : NHibernateRepositoryBase<Guid, TimeZone>, ITimeZoneRepository
    {
        #region ITimeZoneRepository Members

        public TimeZone FindByName(string name)
        {
            return this.FindOne(Expression.Eq("Name", name));
        }

        #endregion
    }
}
