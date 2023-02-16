// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contains extension methods to <see cref="IdentityBuilder"/> for adding entity framework stores.
/// </summary>
public static class BearerBuilderExtensions
{

    /// <summary>
    /// Adds bearer token services.
    /// </summary>
    /// <typeparam name="TToken">The token type</typeparam>
    /// <returns>A <see cref="IdentityBearerTokenBuilder"/> instance.</returns>
    public static IdentityBearerTokenBuilder AddBearerTokens<TToken>(this IdentityBuilder builder)
        where TToken : class

    {
        var tokenManagerType = typeof(TokenManager<,>).MakeGenericType(builder.UserType, typeof(TToken));
        builder.Services.TryAddScoped(tokenManagerType);
        builder.Services.TryAddTransient<IAccessTokenPolicy, JwtAccessTokenPolicy>();
        var ifactory = typeof(IAccessTokenClaimsFactory<>).MakeGenericType(builder.UserType);
        var factory = typeof(AccessTokenClaimsFactory<>).MakeGenericType(builder.UserType);
        builder.Services.TryAddScoped(ifactory, factory);
        // Important to not return a different token manager instance, we only want one scoped token manager
        builder.Services.TryAddScoped(typeof(IAccessTokenValidator), services => services.GetRequiredService(tokenManagerType));
        return new IdentityBearerTokenBuilder(builder, typeof(TToken));
    }

    /// <summary>
    /// Adds an Entity Framework implementation of identity information stores.
    /// </summary>
    /// <typeparam name="TContext">The Entity Framework database context to use.</typeparam>
    /// <param name="builder">The <see cref="IdentityBuilder"/> instance this method extends.</param>
    /// <returns>The <see cref="IdentityBuilder"/> instance this method extends.</returns>
    public static IdentityBuilder AddTokenStore<TContext>(this IdentityBuilder builder)
        where TContext : DbContext
    {
        Type tokenType;
        Type tokenStoreType;
        var identityContext = FindGenericBaseType(typeof(TContext), typeof(IdentityDbContext<,,,,,,,,>));
        if (identityContext == null)
        {
            // If its a custom DbContext, we can only add the default POCOs
            tokenType = typeof(IdentityToken);
            tokenStoreType = typeof(TokenStore<IdentityToken, TContext>);
        }
        else
        {
            tokenType = identityContext.GenericTypeArguments[2];
            tokenStoreType = typeof(TokenStore<,>).MakeGenericType(tokenType, typeof(TContext));
        }

        builder.Services.TryAddScoped(typeof(ITokenStore<>).MakeGenericType(tokenType), tokenStoreType);
        return builder;
    }

    // REVIEW: copied from Identity.EF
    private static Type? FindGenericBaseType(Type currentType, Type genericBaseType)
    {
        Type? type = currentType;
        while (type != null)
        {
            var genericType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
            if (genericType != null && genericType == genericBaseType)
            {
                return type;
            }
            type = type.BaseType;
        }
        return null;
    }
}
