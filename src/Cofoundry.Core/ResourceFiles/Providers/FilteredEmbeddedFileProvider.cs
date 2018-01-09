﻿using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cofoundry.Core.ResourceFiles
{
    /// <summary>
    /// A file provider that wraps an EmbeddedFileProvider and restricts
    /// access to a specific path. Used in tandem with IEmbeddedResourceRouteRegistration
    /// to provide static file access to specific embedded resource paths.
    /// </summary>
    public class FilteredEmbeddedFileProvider : IFileProvider
    {
        private readonly IFileProvider _assemblyProvider;
        private readonly IFileProvider _overrideProvider;
        private readonly string _restrictToPath;

        /// <summary>
        /// A file provider that wraps an EmbeddedFileProvider and restricts
        /// access to a specific path. Used in tandem with IEmbeddedResourceRouteRegistration
        /// to provide static file access to specific embedded resource paths.
        /// </summary>
        /// <param name="assemblyProvider">
        /// An EmbeddedFileProvider instance instantiated with the assembly containing the 
        /// embedded resources to serve.</param>
        /// <param name="filterToPath">The relative file path to restrict file access to e.g. '/parent/child/content'.</param>
        /// <param name="overrideProvider">
        /// A file provider that can contain files that override the assembly provider. Typically
        /// this is a physical file provider for the website root so projects can override files embedded
        /// in Cofoundry with their own versions.
        /// </param>
        public FilteredEmbeddedFileProvider(
            IFileProvider assemblyProvider,
            string filterToPath,
            IFileProvider overrideProvider = null
            )
        {
            if (assemblyProvider == null) throw new ArgumentNullException(nameof(assemblyProvider));
            if (filterToPath == null) throw new ArgumentNullException(nameof(filterToPath));
            if (string.IsNullOrWhiteSpace(filterToPath)) throw new ArgumentEmptyException(nameof(filterToPath));
         
            _restrictToPath = filterToPath.TrimStart('~');

            if (!_restrictToPath.StartsWith("/"))
            {
                throw new ArgumentException(nameof(filterToPath) + " must start with a forward slash.");
            }

            if (_restrictToPath.Length <= 1)
            {
                throw new ArgumentException(nameof(filterToPath) + " cannot be the root directory.");
            }

            _assemblyProvider = assemblyProvider;
            _overrideProvider = overrideProvider;
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            if (subpath == null || !subpath.StartsWith(_restrictToPath))
            {
                return NotFoundDirectoryContents.Singleton;
            }
            
            return _assemblyProvider.GetDirectoryContents(subpath);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            if (string.IsNullOrEmpty(subpath) || !subpath.StartsWith(_restrictToPath, StringComparison.OrdinalIgnoreCase))
            {
                return new NotFoundFileInfo(subpath);
            }

            if (_overrideProvider != null)
            {
                var overrideFile = _overrideProvider.GetFileInfo(subpath);
                if (overrideFile != null && overrideFile.Exists)
                {
                    return overrideFile;
                }
            }

            var fileInfo = _assemblyProvider.GetFileInfo(subpath);
            return fileInfo;
        }

        public IChangeToken Watch(string filter)
        {
            return _assemblyProvider.Watch(filter);
        }
    }
}
