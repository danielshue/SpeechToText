//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Data.Entity;

namespace Azure.Cognitive.Services.Speech.Samples
{
    public class TextTrancriptionContext : DbContext
    {
        public TextTrancriptionContext(string nameOrconnectionString) : base(nameOrconnectionString) { }

        public DbSet<TextTrancription> Trancriptions { get; set; }

    }
}
