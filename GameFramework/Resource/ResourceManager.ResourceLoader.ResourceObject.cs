//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2019 Jiang Yin. All rights reserved.
// Homepage: http://gameframework.cn/
// Feedback: mailto:jiangyin@gameframework.cn
//------------------------------------------------------------

using GameFramework.ObjectPool;
using System.Collections.Generic;

namespace GameFramework.Resource
{
    internal partial class ResourceManager
    {
        private partial class ResourceLoader
        {
            /// <summary>
            /// 资源对象。
            /// </summary>
            private sealed class ResourceObject : ObjectBase
            {
                private readonly List<object> m_DependencyResources;
                private readonly IResourceHelper m_ResourceHelper;
                private readonly Dictionary<object, int> m_ResourceDependencyCount;

                public ResourceObject(string name, object target, IResourceHelper resourceHelper, Dictionary<object, int> resourceDependencyCount)
                    : base(name, target)
                {
                    if (resourceHelper == null)
                    {
                        throw new GameFrameworkException("Resource helper is invalid.");
                    }

                    if (resourceDependencyCount == null)
                    {
                        throw new GameFrameworkException("Resource dependency count is invalid.");
                    }

                    m_DependencyResources = new List<object>();
                    m_ResourceHelper = resourceHelper;
                    m_ResourceDependencyCount = resourceDependencyCount;
                }

                public override bool CustomCanReleaseFlag
                {
                    get
                    {
                        int targetReferenceCount = 0;
                        m_ResourceDependencyCount.TryGetValue(Target, out targetReferenceCount);
                        return base.CustomCanReleaseFlag && targetReferenceCount <= 0;
                    }
                }

                public void AddDependencyResource(object dependencyResource)
                {
                    if (m_DependencyResources.Contains(dependencyResource))
                    {
                        return;
                    }

                    m_DependencyResources.Add(dependencyResource);

                    int referenceCount = 0;
                    if (m_ResourceDependencyCount.TryGetValue(dependencyResource, out referenceCount))
                    {
                        m_ResourceDependencyCount[dependencyResource] = referenceCount + 1;
                    }
                    else
                    {
                        m_ResourceDependencyCount.Add(dependencyResource, 1);
                    }
                }

                protected internal override void Release(bool isShutdown)
                {
                    if (!isShutdown)
                    {
                        int targetReferenceCount = 0;
                        if (m_ResourceDependencyCount.TryGetValue(Target, out targetReferenceCount) && targetReferenceCount > 0)
                        {
                            throw new GameFrameworkException(Utility.Text.Format("Resource target '{0}' reference count is '{1}' larger than 0.", Name, targetReferenceCount.ToString()));
                        }

                        foreach (object dependencyResource in m_DependencyResources)
                        {
                            int referenceCount = 0;
                            if (m_ResourceDependencyCount.TryGetValue(dependencyResource, out referenceCount))
                            {
                                m_ResourceDependencyCount[dependencyResource] = referenceCount - 1;
                            }
                            else
                            {
                                throw new GameFrameworkException(Utility.Text.Format("Resource target '{0}' dependency asset reference count is invalid.", Name));
                            }
                        }
                    }

                    m_ResourceDependencyCount.Remove(Target);
                    m_ResourceHelper.Release(Target);
                }
            }
        }
    }
}
