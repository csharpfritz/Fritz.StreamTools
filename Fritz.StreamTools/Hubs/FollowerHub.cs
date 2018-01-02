using Fritz.StreamTools.Services;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fritz.StreamTools.Hubs
{

  public class FollowerHub : Hub
  {

    public FollowerHub(MyFollowerService myFollowerService)
    {

      this.MyFollowerService = myFollowerService;

    }

    public MyFollowerService MyFollowerService { get; }

    public IObservable<int> StreamFollowers()
    {
      // return _stockTicker.StreamStocks();
      return null;
    }


  }

}
