using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MyGameLib.NetCode.Editor
{
    internal class GhostPostProcessorResolver : Mono.Cecil.IAssemblyResolver
    {
        //local cache to speed up a little the references loading
        struct CacheEntry
        {
            public DateTime lastTimeWrite;
            public Mono.Cecil.AssemblyDefinition assemblyDefinition;
        }

        private Dictionary<string, CacheEntry> _assemblyCache = new Dictionary<string, CacheEntry>();
        public HashSet<string> resolvePaths = new HashSet<string>();

        public void Dispose()
        {
            foreach (var c in _assemblyCache.Values)
            {
                c.assemblyDefinition.Dispose();
            }

            _assemblyCache.Clear();
        }

        private string GetReferenceLocation(string assemblyName)
        {
            return resolvePaths.Select(p => Path.Combine(p, assemblyName + ".dll"))
                .Where(File.Exists).FirstOrDefault();
        }

        public Mono.Cecil.AssemblyDefinition Resolve(Mono.Cecil.AssemblyNameReference name)
        {
            return Resolve(name, new Mono.Cecil.ReaderParameters(Mono.Cecil.ReadingMode.Deferred));
        }

        public Mono.Cecil.AssemblyDefinition Resolve(Mono.Cecil.AssemblyNameReference reference,
            Mono.Cecil.ReaderParameters parameters)
        {
            return ResolveAssemblyByLocation(GetReferenceLocation(reference.Name), parameters);
        }

        public Mono.Cecil.AssemblyDefinition Resolve(string assemblyName, Mono.Cecil.ReaderParameters parameters)
        {
            return ResolveAssemblyByLocation(GetReferenceLocation(assemblyName), parameters);
        }

        public Mono.Cecil.AssemblyDefinition ResolveAssemblyByLocation(string referenceLocation,
            Mono.Cecil.ReaderParameters parameters)
        {
            if (referenceLocation == null)
                return null;

            var assemblyName = Path.GetFileName(referenceLocation);
            var lastTimeWrite = File.GetLastWriteTime(referenceLocation);
            if (_assemblyCache.TryGetValue(assemblyName, out var cacheEntry))
            {
                if (lastTimeWrite == cacheEntry.lastTimeWrite)
                    return cacheEntry.assemblyDefinition;
            }

            parameters.AssemblyResolver = this;

            using (var stream = new MemoryStream(File.ReadAllBytes(referenceLocation)))
            {
                var assemblyDefinition = Mono.Cecil.AssemblyDefinition.ReadAssembly(stream, parameters);
                cacheEntry.assemblyDefinition?.Dispose();
                cacheEntry.assemblyDefinition = assemblyDefinition;
                cacheEntry.lastTimeWrite = lastTimeWrite;
                _assemblyCache[assemblyName] = cacheEntry;
            }

            return cacheEntry.assemblyDefinition;
        }
    }
}