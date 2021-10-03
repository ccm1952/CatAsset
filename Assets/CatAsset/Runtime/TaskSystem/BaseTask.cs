﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatAsset
{
    /// <summary>
    /// 任务基类
    /// </summary>
    public abstract class BaseTask
    {
        protected BaseTask(TaskExcutor owner, string name, int priority = 0,object userData = null)
        {
            this.owner = owner;
            Name = name;
            Priority = priority;
            UserData = userData;
        }

        /// <summary>
        /// 持有此任务的执行器
        /// </summary>

        protected TaskExcutor owner;

        /// <summary>
        /// 任务完成回调
        /// </summary>
        internal virtual Delegate FinishedCallback
        {
            get;
            set;
        }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name;

        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority;

        /// <summary>
        /// 自定义数据
        /// </summary>
        public object UserData;

        /// <summary>
        /// 任务状态
        /// </summary>
        public TaskState State;

        /// <summary>
        /// 任务进度
        /// </summary>
        public virtual float Progress
        {
            get;
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// 刷新任务状态（对任务状态的修改只在此方法中进行）
        /// </summary>
        public abstract void UpdateState();

        public override string ToString()
        {
            return Name;
        }
    }

}
