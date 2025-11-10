using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Consul;
using Ocelot.Logging;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Consul.Interfaces;

namespace GW.Services
{
  public class ConsulService(IHttpContextAccessor contextAccessor, IConsulClientFactory clientFactory, IOcelotLoggerFactory loggerFactory)
: DefaultConsulServiceBuilder(contextAccessor, clientFactory, loggerFactory)
  {
    protected override string GetDownstreamHost(ServiceEntry entry, Node node) => entry.Service.Address;
  }
}