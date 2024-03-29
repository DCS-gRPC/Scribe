﻿using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace RurouniJones.DCScribe.Encyclopedia
{
    public class Unit
    {
        public string Name { get; set; }
        public string Code { get; set; }
        [YamlMember(Alias = "mil_std_2525_d")]
        public string MilStd2525d { get; set; }
        public List<string> DcsCodes { get; set; }
    }
}
