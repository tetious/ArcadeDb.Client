# ArcadeDb.Client

This is a simple ArcadeDb client/driver for .net that wraps the HTTP API. It is inspired by Dapper,
and should (hopefully) feel familiar enough to get started quickly.

## Usage

### ArcadeServer

Wraps the server level operations. (create database/list databases, etc.)

### ArcadeDatabase

Does the interesting bits like queries and serialization.

### (Extras) SimpleRepository

A basic reference document-style repository implementation.

### (Extras) VertexRepository

A start of a sorta-repository that makes it fun to work with vertexes (nodes) and edges (relations) in a type-safe way.
This API will likely change substantially, as it still feels a bit weird to work with.

More documentation to follow once things are a bit more fleshed out. For now the integration tests will hopefully help!

A nuget package is even more forthcoming. Nearly there!
