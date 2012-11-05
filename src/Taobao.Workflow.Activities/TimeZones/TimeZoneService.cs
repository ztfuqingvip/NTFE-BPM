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
using CodeSharp.Core.RepositoryFramework;
using Castle.Services.Transaction;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 时区服务对外接口
    /// </summary>
    public interface ITimeZoneService
    {
        /// <summary>
        /// 通过时区名称获取时区信息
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        TimeZone GetTimeZone(string name);
         /// <summary>
        /// 创建时区信息
        /// </summary>
        /// <param name="type"></param>
        void Create(TimeZone timeZone);
        /// <summary>
        /// 更新时区
        /// </summary>
        /// <param name="timeZone"></param>
        void Update(TimeZone timeZone);
        /// <summary>
        /// 删除时区
        /// </summary>
        /// <param name="timeZone"></param>
        void Delete(TimeZone timeZone);
    }
    /// <summary>
    /// 时区服务
    /// </summary>
    [Transactional]
    public class TimeZoneService : ITimeZoneService
    {
        private static ITimeZoneRepository _repository;
        static TimeZoneService()
        {
            _repository = RepositoryFactory.GetRepository<ITimeZoneRepository, Guid, TimeZone>();
        }

        #region ITimeZoneService Members
        TimeZone ITimeZoneService.GetTimeZone(string name)
        {
            return _repository.FindByName(name);
        }

        [Transaction(TransactionMode.Requires)]
        void ITimeZoneService.Create(TimeZone timeZone)
        {
            if (_repository.FindByName(timeZone.Name) != null)
                throw new InvalidOperationException("已经存在同名的时区信息");
            _repository.Add(timeZone);
        }

        [Transaction(TransactionMode.Requires)]
        void ITimeZoneService.Update(TimeZone timeZone)
        {
            _repository.Update(timeZone);
        }

        [Transaction(TransactionMode.Requires)]
        void ITimeZoneService.Delete(TimeZone timeZone)
        {
            _repository.Remove(timeZone);
        }
        #endregion
    }
}