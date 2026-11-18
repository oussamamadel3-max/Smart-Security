import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:smart_security_app_core/smart_security_app_core.dart';

import '../../../../core/widgets/owner_visual.dart';
import '../admin_copy.dart';

class AdminPanel extends StatelessWidget {
  const AdminPanel({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: DesignTokens.card,
        borderRadius: BorderRadius.circular(DesignTokens.radiusXl),
        border: Border.all(color: DesignTokens.border),
        boxShadow: DesignTokens.cardShadow,
      ),
      child: child,
    );
  }
}

enum AdminNoticeTone { warning, info, error }

class AdminNotice extends StatelessWidget {
  const AdminNotice({
    super.key,
    required this.text,
    this.danger = false,
    this.tone,
  });

  final String text;
  final bool danger;
  final AdminNoticeTone? tone;

  @override
  Widget build(BuildContext context) {
    final resolved = danger
        ? AdminNoticeTone.error
        : (tone ?? AdminNoticeTone.warning);
    final (bg, fg, border) = switch (resolved) {
      AdminNoticeTone.info => (
        DesignTokens.sky,
        DesignTokens.infoSoftText,
        DesignTokens.border,
      ),
      AdminNoticeTone.error => (
        DesignTokens.dangerSoftBg,
        DesignTokens.dangerSoftText,
        DesignTokens.dangerSoftText,
      ),
      AdminNoticeTone.warning => (
        DesignTokens.noticeBg,
        DesignTokens.noticeText,
        DesignTokens.noticeBorder,
      ),
    };
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(bottom: 17),
      padding: const EdgeInsets.symmetric(horizontal: 15, vertical: 13),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(11),
        border: Border.all(color: border),
      ),
      child: Text(
        text,
        style: Theme.of(
          context,
        ).textTheme.bodySmall?.copyWith(color: fg, height: 1.9),
      ),
    );
  }
}

class AdminField extends StatelessWidget {
  const AdminField({
    super.key,
    required this.label,
    required this.controller,
    this.obscure = false,
    this.maxLines = 1,
    this.keyboardType,
    this.errorText,
  });

  final String label;
  final TextEditingController controller;
  final bool obscure;
  final int maxLines;
  final TextInputType? keyboardType;
  final String? errorText;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 13),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: Theme.of(context).textTheme.labelMedium?.copyWith(
              color: DesignTokens.muted,
              fontWeight: FontWeight.w700,
            ),
          ),
          const SizedBox(height: 6),
          TextField(
            controller: controller,
            obscureText: obscure,
            maxLines: obscure ? 1 : maxLines,
            keyboardType: keyboardType,
            decoration: InputDecoration(errorText: errorText),
          ),
        ],
      ),
    );
  }
}

String? adminFieldError({
  required String key,
  required String? field,
  required String? message,
}) {
  if (field != key || message == null || message.isEmpty) return null;
  return message;
}

class AdminPageBody extends StatelessWidget {
  const AdminPageBody({
    super.key,
    required this.title,
    required this.subtitle,
    required this.child,
    this.action,
  });

  final String title;
  final String subtitle;
  final Widget child;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    final width = MediaQuery.sizeOf(context).width;
    return Align(
      alignment: Alignment.topCenter,
      child: ConstrainedBox(
        constraints: const BoxConstraints(
          maxWidth: OwnerLayout.contentMaxWidth,
        ),
        child: ListView(
          padding: OwnerLayout.pagePadding(width),
          children: [
            OwnerPageHeader(
              title: title,
              subtitle: subtitle,
              eyebrow: AdminCopy.eyebrow,
              trailing: action,
            ),
            child,
          ],
        ),
      ),
    );
  }
}

class AdminFormFrame extends StatelessWidget {
  const AdminFormFrame({super.key, required this.child, this.maxWidth = 750});

  final Widget child;
  final double maxWidth;

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: AlignmentDirectional.centerStart,
      child: ConstrainedBox(
        constraints: BoxConstraints(maxWidth: maxWidth),
        child: child,
      ),
    );
  }
}

class AdminKvGrid extends StatelessWidget {
  const AdminKvGrid({super.key, required this.entries});

  final List<(String, String)> entries;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final two = constraints.maxWidth >= 560;
        const gap = 12.0;
        final width = two
            ? (constraints.maxWidth - gap) / 2
            : constraints.maxWidth;
        return Wrap(
          spacing: gap,
          runSpacing: gap,
          children: [
            for (final entry in entries)
              SizedBox(
                width: width.clamp(0, constraints.maxWidth),
                child: DecoratedBox(
                  decoration: BoxDecoration(
                    color: AppColors.lightSurfaceVariant,
                    borderRadius: BorderRadius.circular(11),
                  ),
                  child: Padding(
                    padding: const EdgeInsets.all(14),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          entry.$1,
                          style: Theme.of(context).textTheme.labelSmall
                              ?.copyWith(color: DesignTokens.muted),
                        ),
                        const SizedBox(height: 6),
                        SelectableText(
                          entry.$2,
                          style: Theme.of(context).textTheme.labelLarge
                              ?.copyWith(color: DesignTokens.ink),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
          ],
        );
      },
    );
  }
}

class AdminSectionTitle extends StatelessWidget {
  const AdminSectionTitle({super.key, required this.title, this.action});

  final String title;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 17),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Text(
              title,
              maxLines: 3,
              overflow: TextOverflow.ellipsis,
              style: Theme.of(context).textTheme.titleLarge,
            ),
          ),
          if (action != null) ...[const SizedBox(width: 8), action!],
        ],
      ),
    );
  }
}

enum AdminPillTone { green, red, blue, amber, gray }

class AdminPill extends StatelessWidget {
  const AdminPill(this.label, this.tone, {super.key});

  final String label;
  final AdminPillTone tone;

  @override
  Widget build(BuildContext context) {
    final (bg, fg) = switch (tone) {
      AdminPillTone.green => (
        DesignTokens.successSoftBg,
        DesignTokens.successSoftText,
      ),
      AdminPillTone.red => (
        DesignTokens.dangerSoftBg,
        DesignTokens.dangerSoftText,
      ),
      AdminPillTone.blue => (
        DesignTokens.infoSoftBg,
        DesignTokens.infoSoftText,
      ),
      AdminPillTone.amber => (
        DesignTokens.warningSoftBg,
        DesignTokens.warningSoftText,
      ),
      AdminPillTone.gray => (AppColors.lightSurfaceVariant, DesignTokens.muted),
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(DesignTokens.radiusPill),
      ),
      child: Text(
        label,
        style: Theme.of(context).textTheme.labelSmall?.copyWith(
          color: fg,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

AdminPill adminAccountStatusPill(String code) {
  return AdminPill(AdminCopy.accountStatus(code), switch (code) {
    'DISABLED' => AdminPillTone.red,
    'ARCHIVED' => AdminPillTone.gray,
    _ => AdminPillTone.green,
  });
}

abstract final class AdminButtonStyles {
  static ButtonStyle get primary => FilledButton.styleFrom(
    backgroundColor: DesignTokens.primary,
    foregroundColor: DesignTokens.white,
    disabledBackgroundColor: DesignTokens.primary.withValues(alpha: 0.45),
    disabledForegroundColor: DesignTokens.white,
    elevation: 0,
    padding: const EdgeInsets.symmetric(horizontal: 17, vertical: 10),
    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
    textStyle: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700),
  );

  static ButtonStyle get secondary => FilledButton.styleFrom(
    backgroundColor: DesignTokens.primarySoft,
    foregroundColor: DesignTokens.primaryTextSoft,
    disabledBackgroundColor: DesignTokens.primarySoft.withValues(alpha: 0.45),
    disabledForegroundColor: DesignTokens.primaryTextSoft,
    elevation: 0,
    padding: const EdgeInsets.symmetric(horizontal: 17, vertical: 10),
    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
    textStyle: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700),
  );

  static ButtonStyle get danger => FilledButton.styleFrom(
    backgroundColor: DesignTokens.dangerSoftBg,
    foregroundColor: DesignTokens.dangerSoftText,
    disabledBackgroundColor: DesignTokens.dangerSoftBg.withValues(alpha: 0.45),
    disabledForegroundColor: DesignTokens.dangerSoftText,
    elevation: 0,
    padding: const EdgeInsets.symmetric(horizontal: 17, vertical: 10),
    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
    textStyle: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700),
  );

  static ButtonStyle get outline => OutlinedButton.styleFrom(
    foregroundColor: DesignTokens.outlineButton,
    disabledForegroundColor: DesignTokens.muted,
    side: const BorderSide(color: Color(0xFFDCE7F2)),
    padding: const EdgeInsets.symmetric(horizontal: 17, vertical: 10),
    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
    textStyle: const TextStyle(fontSize: 12, fontWeight: FontWeight.w700),
  );
}

class AdminTextLink extends StatelessWidget {
  const AdminTextLink({
    super.key,
    required this.label,
    required this.onPressed,
  });

  final String label;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return TextButton(
      style: TextButton.styleFrom(
        foregroundColor: DesignTokens.primary,
        padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 8),
        minimumSize: const Size(44, 32),
        tapTargetSize: MaterialTapTargetSize.padded,
        textStyle: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700),
      ),
      onPressed: onPressed,
      child: Text(label),
    );
  }
}

class AdminCommandRow extends StatelessWidget {
  const AdminCommandRow({super.key, required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 17),
      child: Wrap(spacing: 10, runSpacing: 10, children: children),
    );
  }
}

class AdminTable extends StatelessWidget {
  const AdminTable({
    super.key,
    required this.headers,
    required this.rows,
    this.emptyLabel = AdminCopy.noMatches,
  });

  final List<String> headers;
  final List<List<Widget>> rows;
  final String emptyLabel;

  @override
  Widget build(BuildContext context) {
    final headerStyle = Theme.of(context).textTheme.labelSmall?.copyWith(
      color: DesignTokens.muted,
      fontWeight: FontWeight.w700,
    );
    return LayoutBuilder(
      builder: (context, constraints) {
        final minWidth = headers.length * 132.0;
        final width = constraints.maxWidth < minWidth
            ? minWidth
            : constraints.maxWidth;
        return SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: SizedBox(
            width: width,
            child: Table(
              defaultVerticalAlignment: TableCellVerticalAlignment.middle,
              children: [
                TableRow(
                  decoration: const BoxDecoration(
                    color: AppColors.lightSurfaceVariant,
                  ),
                  children: [
                    for (final header in headers)
                      Padding(
                        padding: const EdgeInsets.all(13),
                        child: Text(
                          header,
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                          style: headerStyle,
                        ),
                      ),
                  ],
                ),
                if (rows.isEmpty)
                  TableRow(
                    children: [
                      Padding(
                        padding: const EdgeInsets.all(14),
                        child: Text(emptyLabel),
                      ),
                      for (var i = 1; i < headers.length; i++)
                        const SizedBox.shrink(),
                    ],
                  )
                else
                  for (final row in rows)
                    TableRow(
                      decoration: const BoxDecoration(
                        border: Border(
                          bottom: BorderSide(color: AppColors.lightDivider),
                        ),
                      ),
                      children: [
                        for (final cell in row)
                          Padding(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 13,
                              vertical: 14,
                            ),
                            child: DefaultTextStyle.merge(
                              maxLines: 2,
                              overflow: TextOverflow.ellipsis,
                              child: cell,
                            ),
                          ),
                      ],
                    ),
              ],
            ),
          ),
        );
      },
    );
  }
}

class AdminSearchField extends StatelessWidget {
  const AdminSearchField({
    super.key,
    required this.hint,
    this.controller,
    this.onChanged,
    this.onSubmitted,
  });

  final String hint;
  final TextEditingController? controller;
  final ValueChanged<String>? onChanged;
  final ValueChanged<String>? onSubmitted;

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      onChanged: onChanged,
      onSubmitted: onSubmitted,
      decoration: InputDecoration(hintText: hint),
    );
  }
}

class AdminFilterMenu extends StatelessWidget {
  const AdminFilterMenu({
    super.key,
    required this.value,
    required this.options,
    required this.onChanged,
  });

  final String value;
  final List<String> options;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: DesignTokens.card,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: const Color(0xFFDFE7F0)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12),
        child: DropdownButtonHideUnderline(
          child: DropdownButton<String>(
            value: value,
            isExpanded: true,
            borderRadius: BorderRadius.circular(10),
            items: [
              for (final option in options)
                DropdownMenuItem(value: option, child: Text(option)),
            ],
            onChanged: (next) {
              if (next != null) onChanged(next);
            },
          ),
        ),
      ),
    );
  }
}

class AdminToolbar extends StatelessWidget {
  const AdminToolbar({super.key, required this.search, this.filter});

  final Widget search;
  final Widget? filter;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 17),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final stacked = constraints.maxWidth < 560;
          if (stacked) {
            return Column(
              children: [
                search,
                if (filter != null) ...[
                  const SizedBox(height: 9),
                  SizedBox(height: 46, child: filter),
                ],
              ],
            );
          }
          return Row(
            children: [
              Expanded(child: search),
              if (filter != null) ...[
                const SizedBox(width: 9),
                SizedBox(width: 160, height: 46, child: filter),
              ],
            ],
          );
        },
      ),
    );
  }
}

class AdminPager extends StatelessWidget {
  const AdminPager({
    super.key,
    required this.page,
    required this.total,
    required this.enabled,
    required this.hasPrevious,
    required this.hasNext,
    required this.onPrevious,
    required this.onNext,
  });

  final int page;
  final int total;
  final bool enabled;
  final bool hasPrevious;
  final bool hasNext;
  final VoidCallback onPrevious;
  final VoidCallback onNext;

  @override
  Widget build(BuildContext context) {
    final label = Text('${AdminCopy.page} $page · ${AdminCopy.total} $total');
    final previous = OutlinedButton(
      style: AdminButtonStyles.outline,
      onPressed: enabled && hasPrevious ? onPrevious : null,
      child: const Text(AdminCopy.previous),
    );
    final next = OutlinedButton(
      style: AdminButtonStyles.outline,
      onPressed: enabled && hasNext ? onNext : null,
      child: const Text(AdminCopy.next),
    );
    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth < 520) {
          return Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              label,
              const SizedBox(height: 8),
              Wrap(spacing: 8, runSpacing: 8, children: [previous, next]),
            ],
          );
        }
        return Row(
          children: [
            label,
            const Spacer(),
            previous,
            const SizedBox(width: 8),
            next,
          ],
        );
      },
    );
  }
}

class AdminProcessSteps extends StatelessWidget {
  const AdminProcessSteps({super.key});

  @override
  Widget build(BuildContext context) {
    return AdminPanel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            AdminCopy.processPreview,
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 17),
          const Wrap(
            spacing: 12,
            runSpacing: 12,
            children: [
              _ProcessStep(AdminCopy.stepRequest, AdminCopy.stepRequestHint),
              _ProcessStep(AdminCopy.stepPending, AdminCopy.stepPendingHint),
              _ProcessStep(AdminCopy.stepOutcome, AdminCopy.stepOutcomeHint),
            ],
          ),
        ],
      ),
    );
  }
}

class _ProcessStep extends StatelessWidget {
  const _ProcessStep(this.title, this.hint);

  final String title;
  final String hint;

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: const BoxConstraints(minWidth: 120, maxWidth: 240),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: AppColors.lightSurfaceVariant,
        borderRadius: BorderRadius.circular(11),
      ),
      child: Column(
        children: [
          Text(title, style: Theme.of(context).textTheme.labelLarge),
          Text(
            hint,
            style: Theme.of(
              context,
            ).textTheme.labelMedium?.copyWith(color: DesignTokens.muted),
          ),
        ],
      ),
    );
  }
}

class AdminTimelineTile extends StatelessWidget {
  const AdminTimelineTile({
    super.key,
    required this.symbol,
    required this.title,
    required this.subtitle,
    required this.time,
    this.onTap,
  });

  final String symbol;
  final String title;
  final String subtitle;
  final String time;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final row = Padding(
      padding: const EdgeInsets.symmetric(vertical: 13),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final icon = Container(
            width: 37,
            height: 37,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: DesignTokens.dangerSoftBg,
              borderRadius: BorderRadius.circular(11),
            ),
            child: Text(
              symbol,
              style: const TextStyle(color: DesignTokens.danger, fontSize: 16),
            ),
          );
          final copy = Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(context).textTheme.labelLarge,
              ),
              const SizedBox(height: 3),
              Text(
                subtitle,
                maxLines: 3,
                overflow: TextOverflow.ellipsis,
                style: Theme.of(
                  context,
                ).textTheme.bodySmall?.copyWith(color: DesignTokens.muted),
              ),
            ],
          );
          final stamp = Text(
            time,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: Theme.of(
              context,
            ).textTheme.labelSmall?.copyWith(color: DesignTokens.muted),
          );
          if (constraints.maxWidth < 480) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    icon,
                    const SizedBox(width: 12),
                    Expanded(child: copy),
                  ],
                ),
                const SizedBox(height: 6),
                stamp,
              ],
            );
          }
          return Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              icon,
              const SizedBox(width: 12),
              Expanded(child: copy),
              const SizedBox(width: 12),
              ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 148),
                child: stamp,
              ),
            ],
          );
        },
      ),
    );
    if (onTap == null) return row;
    return InkWell(onTap: onTap, child: row);
  }
}

class AdminMetricGrid extends StatelessWidget {
  const AdminMetricGrid({super.key, required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final columns = OwnerLayout.metricColumns(constraints.maxWidth);
        const gap = 16.0;
        final width = (constraints.maxWidth - gap * (columns - 1)) / columns;
        return Wrap(
          spacing: gap,
          runSpacing: gap,
          children: [
            for (final child in children)
              SizedBox(
                width: width.clamp(0, constraints.maxWidth),
                child: child,
              ),
          ],
        );
      },
    );
  }
}

class AdminMetricCard extends StatelessWidget {
  const AdminMetricCard({
    super.key,
    required this.title,
    required this.value,
    required this.hint,
    required this.symbol,
  });

  final String title;
  final String value;
  final String hint;
  final String symbol;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      constraints: const BoxConstraints(minHeight: 128),
      padding: const EdgeInsets.fromLTRB(20, 19, 20, 19),
      decoration: BoxDecoration(
        color: DesignTokens.card,
        borderRadius: BorderRadius.circular(15),
        border: Border.all(color: DesignTokens.border),
        boxShadow: DesignTokens.cardShadow,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Text(
                  title,
                  style: theme.textTheme.labelMedium?.copyWith(
                    color: DesignTokens.muted,
                  ),
                ),
              ),
              Container(
                width: 40,
                height: 40,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: DesignTokens.infoSoftBg,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  symbol,
                  style: const TextStyle(
                    fontSize: 20,
                    color: DesignTokens.primary,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 13),
          Text(
            value,
            style: theme.textTheme.headlineMedium?.copyWith(
              fontSize: 28,
              fontWeight: FontWeight.w800,
              height: 1.1,
              color: DesignTokens.ink,
            ),
          ),
          const SizedBox(height: 5),
          Text(
            hint,
            style: theme.textTheme.labelMedium?.copyWith(
              color: DesignTokens.muted,
            ),
          ),
        ],
      ),
    );
  }
}

Future<bool?> showAdminConfirmDialog({
  required BuildContext context,
  required String title,
  required String message,
  bool danger = false,
}) {
  return showDialog<bool>(
    context: context,
    barrierColor: const Color(0x90102137),
    builder: (context) {
      return Dialog(
        backgroundColor: DesignTokens.card,
        insetPadding: const EdgeInsets.all(15),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(DesignTokens.radiusModal),
        ),
        child: SingleChildScrollView(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 490),
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(title, style: Theme.of(context).textTheme.headlineSmall),
                  const SizedBox(height: 5),
                  Text(
                    message,
                    style: Theme.of(
                      context,
                    ).textTheme.bodySmall?.copyWith(color: DesignTokens.muted),
                  ),
                  const SizedBox(height: 19),
                  const AdminNotice(text: AdminCopy.serverConfirms),
                  Wrap(
                    spacing: 9,
                    runSpacing: 9,
                    children: [
                      FilledButton(
                        style: danger
                            ? AdminButtonStyles.danger
                            : AdminButtonStyles.primary,
                        onPressed: () => Navigator.of(context).pop(true),
                        child: const Text(AdminCopy.confirm),
                      ),
                      OutlinedButton(
                        style: AdminButtonStyles.outline,
                        onPressed: () => Navigator.of(context).pop(false),
                        child: const Text(AdminCopy.cancel),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),
      );
    },
  );
}

/// Rebuilds when any watched field changes so leave guards see dirty input.
class AdminFormScope extends StatefulWidget {
  const AdminFormScope({
    super.key,
    required this.fields,
    required this.builder,
  });

  final List<TextEditingController> fields;
  final Widget Function(BuildContext context, bool dirty) builder;

  @override
  State<AdminFormScope> createState() => _AdminFormScopeState();
}

class _AdminFormScopeState extends State<AdminFormScope> {
  @override
  void initState() {
    super.initState();
    for (final field in widget.fields) {
      field.addListener(_rebuild);
    }
  }

  @override
  void dispose() {
    for (final field in widget.fields) {
      field.removeListener(_rebuild);
    }
    super.dispose();
  }

  void _rebuild() {
    if (mounted) setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    final dirty = widget.fields.any((field) => field.text.trim().isNotEmpty);
    return widget.builder(context, dirty);
  }
}

/// Blocks browser/system back while [dirty] until the user confirms.
class AdminLeaveGuard extends StatefulWidget {
  const AdminLeaveGuard({super.key, required this.dirty, required this.child});

  final bool dirty;
  final Widget child;

  @override
  State<AdminLeaveGuard> createState() => _AdminLeaveGuardState();
}

class _AdminLeaveGuardState extends State<AdminLeaveGuard> {
  bool _allow = false;

  @override
  Widget build(BuildContext context) {
    return PopScope(
      canPop: !widget.dirty || _allow,
      onPopInvokedWithResult: (didPop, _) async {
        if (didPop || _allow || !widget.dirty) return;
        final ok = await confirmAdminDiscard(context);
        if (!ok || !mounted) return;
        setState(() => _allow = true);
        WidgetsBinding.instance.addPostFrameCallback((_) {
          if (mounted) Navigator.of(context).pop();
        });
      },
      child: widget.child,
    );
  }
}

Future<bool> confirmAdminDiscard(BuildContext context) async {
  final result = await showAdminConfirmDialog(
    context: context,
    title: AdminCopy.discardTitle,
    message: AdminCopy.discardBody,
  );
  return result == true;
}

Future<void> leaveAdminForm(
  BuildContext context, {
  required bool dirty,
  required String location,
}) async {
  if (dirty) {
    final ok = await confirmAdminDiscard(context);
    if (!ok || !context.mounted) return;
  }
  context.go(location);
}

String formatAdminTime(DateTime? value) {
  if (value == null) return '—';
  final local = value.toLocal();
  String two(int n) => n.toString().padLeft(2, '0');
  return '${local.year}-${two(local.month)}-${two(local.day)} '
      '${two(local.hour)}:${two(local.minute)}';
}
