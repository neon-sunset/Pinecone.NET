## General
- [ ] Finish implementation, ~~look into pooling vector arrays to reduce memory pressure / allocation cost~~ (alternate approach - not pool but directly wrap arrays supplied by user for gRPC payloads)
- [ ] Switch from hand-written approach to code generation for REST API
- [ ] Switch from Golang library .proto contract to the Rust one because the former has a dependency/import graph
    which hits an edge case in protoc-csharp plugin preventing it from being usable
- [ ] Test coverage

## Impl
- [ ] Devise a good container implementatioon for dynamically-ish typed metadata objects used for filtering. Should I use 'OneOf' nuget package?
- [ ] Debug actual Pinecone API behavior when querying vectors, there seem to be multiple conflicting(?) fields in QueryResponse (although Rust impl. suggests that only .Matches is valid)