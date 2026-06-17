# P2-FE-003 Checklist

- [ ] Red: failing tests reproduced the missing-sidebar scenario for a user without `sys:menu:tree`
- [ ] Green: auth endpoints now return visible menu trees
- [ ] Refactor: no duplicated menu-tree building logic remains without justification
- [ ] `LoginResponse` and `AuthMeResponse` OpenAPI schemas expose `MenuTreeDto[]`
- [ ] Frontend navigation still works for users with `sys:menu:tree`
- [ ] Frontend navigation now works for users without `sys:menu:tree`
- [ ] Backend quality gates passed
- [ ] Frontend quality gates passed
- [ ] Current-task audit passed
- [ ] Final scope audit passed
