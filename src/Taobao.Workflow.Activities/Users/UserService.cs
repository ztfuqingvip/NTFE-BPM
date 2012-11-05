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
    /// 用户服务对外接口
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// 获取用户信息，不存在则创建
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        User GetUserWhatever(string userName);
        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        User GetUser(string userName);
        /// <summary>
        /// 创建用户
        /// </summary>
        /// <param name="user"></param>
        void Create(User user);
    }
    /// <summary>
    /// 用户服务
    /// </summary>
    [Transactional]
    public class UserService : IUserService
    {
        private static IUserRepository _repository;
        static UserService()
        {
            _repository = RepositoryFactory.GetRepository<IUserRepository, Guid, User>();
        }

        #region IUserService Members
        [Transaction(TransactionMode.Requires)]
        User IUserService.GetUserWhatever(string userName)
        {
            var user = _repository.FindByUserName(userName);
            if (user == null)
                _repository.Add(user = new User(userName));
            return user;
        }
        User IUserService.GetUser(string userName)
        {
            return _repository.FindByUserName(userName);
        }
        [Transaction(TransactionMode.Supported)]
        void IUserService.Create(User user)
        {
            if (_repository.FindByUserName(user.UserName) == null)
                _repository.Add(user);
        }
        #endregion
    }
}
