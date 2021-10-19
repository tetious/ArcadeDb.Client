# ArcadeDb.Client

This is a simple ArcadeDb client/driver for .net that wraps the HTTP API. It is
inspired by Dapper, and should (hopefully) feel familiar enough to get started
quickly.

# Features

- Easy to understand API, inspired by Dapper, with graphy enhancements.
- Full transaction support. (requires ArcadeDb v21.10.2 or later)
- Hopefully sensible nearly stateless design.
- Quickly parsed, tersely written documentation. (you're reading it now!)

# Usage

## ArcadeDb.Client

### ArcadeServer

Wraps the server level operations. (create database/list databases, etc.)

### ArcadeDatabase

Does the interesting bits like queries and serialization. Includes support for
transactions. 

## ArcadeDb.Extras

Collects various bits and bobs that are either out of scope for a
general-purpose database client, or have an unstable API.

### UnitOfWork

A database level transaction wrapper that has various convenient options for
working with strongly typed data. This will get more fleshed out with linking
features soon.

### SimpleRepository

A basic reference document-style repository implementation.

### VertexRepository

A start of a sorta-repository that makes it fun to work with vertexes (nodes)
and edges (relations) in a type-safe way. This API will likely change
substantially, as it still feels a bit weird to work with.

More documentation to follow once things are a bit more fleshed out. For now the
integration tests will hopefully help!

A nuget package is even more forthcoming. Nearly there!
