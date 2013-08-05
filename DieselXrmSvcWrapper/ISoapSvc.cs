﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace DieselXrmSvcWrapper
{
    [ServiceContract]
    public interface ISoapSvc
    {
        [OperationContract]
        List<List<ParameterItem>> Retrieve(string query, List<ParameterItem> inputParameters);

    }

    [DataContract]
    public class ParameterItem
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public object Value { get; set; }
    }

}
