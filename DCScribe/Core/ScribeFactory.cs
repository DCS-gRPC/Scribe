using System;

namespace RurouniJones.DCScribe.Core
{
    public class ScribeFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ScribeFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Scribe CreateScribe()
        {
            return (Scribe) _serviceProvider.GetService(typeof(Scribe));
        }
    }
}