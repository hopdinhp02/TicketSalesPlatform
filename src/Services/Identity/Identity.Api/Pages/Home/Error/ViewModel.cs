// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Models;

namespace TicketSalesPlatform.Identity.Api.Pages.Error;

public class ViewModel
{
    public ViewModel()
    {
    }

    public ViewModel(string error)
    {
        Error = new ErrorMessage { Error = error };
    }

    public ErrorMessage? Error { get; set; }
}