/// One server page. [totalCount] is the API total, not the loaded row count.
class PagedPage<T> {
  const PagedPage({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalCount,
  });

  final List<T> items;
  final int page;
  final int pageSize;
  final int totalCount;

  bool get hasPrevious => page > 1;

  bool get hasNext => pageSize > 0 && page * pageSize < totalCount;
}
