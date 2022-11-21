# Release process

1. Build the plugin.
2. If there is no release branch yet for the current version:  
   `nbgv prepare-release`
3. Create a tag and push it afterwards:
    - `nbgv tag`
    - `git push origin v1.2`
4. Create a release in GitHub from the tag and attach the file `PropertyServer.dll`.
5. Push the release branch and the main branch.
