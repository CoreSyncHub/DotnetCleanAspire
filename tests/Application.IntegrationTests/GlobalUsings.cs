// Persistence
global using Infrastructure.Persistence;
global using Microsoft.EntityFrameworkCore;

// Application abstractions
global using Application.Abstractions.Messaging;
global using Application.Abstractions.Caching;
global using Application.Abstractions.Identity;
global using Application.Abstractions.Identity.Dtos;
global using Application.Abstractions.Helpers;
global using Application.Abstractions.Pagination;

// Domain
global using Domain.Abstractions;

// Features
global using Application.Features.Todos.Commands.CreateTodo;
global using Application.Features.Todos.Commands.CompleteTodo;
global using Application.Features.Todos.Queries.GetTodoById;
global using Application.Features.Todos.Queries.GetTodos;
global using Application.Features.Todos.Dtos;
global using Application.Features.Auth.Commands.Register;
global using Application.Features.Auth.Commands.Login;
global using Application.Features.Auth.Commands.RefreshToken;
global using Application.Features.Auth.Commands.Logout;
global using Application.Features.Auth.Commands.ForgotPassword;
global using Application.Features.Auth.Commands.ResetPassword;
global using Application.Features.Auth.Commands.ExchangeCode;
global using Application.Features.Auth.Queries.GetCurrentUser;
global using Application.Features.Auth.Errors;

// Identity entities (used in auth test assertions)
global using Infrastructure.Identity.Entities;

// Fixtures
global using Application.IntegrationTests.Fixtures;
global using Application.IntegrationTests.Base;

// Microsoft DI
global using Microsoft.Extensions.DependencyInjection;
