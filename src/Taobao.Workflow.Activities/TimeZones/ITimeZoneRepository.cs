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

namespace Taobao.Workflow.Activities
{
    public interface ITimeZoneRepository : CodeSharp.Core.RepositoryFramework.IRepository<Guid, TimeZone>
    {
        /// <summary>
        /// 通过时区名称获取时区信息
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        TimeZone FindByName(string name);
    }
}
