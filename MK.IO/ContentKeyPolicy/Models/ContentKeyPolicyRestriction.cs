﻿using JsonSubTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MK.IO
{
    [JsonConverter(typeof(JsonSubtypes), "@odata.type")]
    [JsonSubtypes.KnownSubType(typeof(ContentKeyPolicyTokenRestriction), "#Microsoft.Media.ContentKeyPolicyTokenRestriction")]
    [JsonSubtypes.KnownSubType(typeof(ContentKeyPolicyOpenRestriction), "#Microsoft.Media.ContentKeyPolicyOpenRestriction")]


    //
    // Summary:
    //     Base class for Content Key Policy restrictions. A derived class must be used
    //     to create a restriction.
    public class ContentKeyPolicyRestriction
    {

        [JsonProperty("@odata.type")]
        public virtual string OdataType { get; set; }
    }
}