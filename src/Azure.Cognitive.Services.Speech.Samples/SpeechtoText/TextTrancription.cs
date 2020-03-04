//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.ComponentModel.DataAnnotations;

namespace Azure.Cognitive.Services.Speech.Samples
{
    public class TextTrancription
    {
        [Key]
        public int Id { get; set; }

        public DateTime CreationDate { get; set; }

        public string Name { get; set; }

        public string Trancription { get; set; }

        public string KeyPhrases { get; set; }

        public string Sentiments { get; set; }

        public double ProcessTime { get; set; }
    }
}
