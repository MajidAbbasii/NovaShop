"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter, usePathname, useSearchParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { ImageUploader } from "@/components/admin/image-uploader";
import { useLocale } from "@/lib/locale-context";
import { isAuthenticated } from "@/lib/cart-api";
import { redirectToLogin } from "@/lib/auth-context";
import { createCustomDollRequest } from "@/lib/custom-doll-api";
import { toast } from "@/hooks/use-toast";
import { Camera, Loader2, ListChecks } from "lucide-react";

export default function NewCustomDollRequestPage() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { t, dir } = useLocale();
  const [imageUrl, setImageUrl] = useState<string>("");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [bodyColor, setBodyColor] = useState("");
  const [eyeColor, setEyeColor] = useState("");
  const [height, setHeight] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [authed, setAuthed] = useState(false);

  useEffect(() => {
    if (!isAuthenticated()) {
      redirectToLogin(router, pathname, searchParams);
      return;
    }
    setAuthed(true);
  }, [router, pathname, searchParams]);

  if (!authed) return null;

  const steps = [
    { key: "step1", icon: Camera },
    { key: "step2", icon: ListChecks },
    { key: "step3", icon: ListChecks },
    { key: "step4", icon: Camera },
  ];

  const submit = async () => {
    if (!imageUrl) {
      toast({ title: t("customDoll.imageRequired"), variant: "destructive" });
      return;
    }
    if (!title.trim()) {
      toast({ title: t("customDoll.titleRequired"), variant: "destructive" });
      return;
    }
    if (!bodyColor) {
      toast({ title: t("customDoll.bodyColorRequired"), variant: "destructive" });
      return;
    }
    if (!eyeColor) {
      toast({ title: t("customDoll.eyeColorRequired"), variant: "destructive" });
      return;
    }
    if (!height || parseInt(height) <= 0) {
      toast({ title: t("customDoll.heightRequired"), variant: "destructive" });
      return;
    }
    setSubmitting(true);
    try {
      const id = await createCustomDollRequest(
        imageUrl,
        title.trim(),
        description.trim(),
        bodyColor,
        eyeColor,
        parseInt(height)
      );
      toast({ title: t("customDoll.created") });
      router.push(`/custom-doll-requests/${id}`);
    } catch (e) {
      toast({
        title: t("customDoll.uploadError"),
        description: e instanceof Error ? e.message : undefined,
        variant: "destructive",
      });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-amber-50/30 py-10">
      <div className="mx-auto max-w-2xl px-4 sm:px-6 lg:px-8">
        <div className={`mb-6 flex items-center justify-between ${dir === "rtl" ? "flex-row-reverse" : ""}`}>
          <h1 className="flex items-center gap-2 text-2xl font-bold">
            <Camera className="size-6 text-primary" />
            {t("customDoll.title")}
          </h1>
          <Button variant="outline" size="sm" asChild>
            <Link href="/custom-doll-requests">
              <ListChecks className="size-4" />
              {t("customDoll.myRequests")}
            </Link>
          </Button>
        </div>

        <p className="mb-6 text-sm text-muted-foreground">{t("customDoll.subtitle")}</p>

        {/* Process steps */}
        <div className="mb-8 grid grid-cols-2 gap-3 sm:grid-cols-4">
          {steps.map((s, i) => (
            <div key={s.key} className="rounded-2xl border bg-card p-4 text-center shadow-sm">
              <div className="mx-auto mb-2 flex size-8 items-center justify-center rounded-full bg-primary/10 text-sm font-bold text-primary">
                {i + 1}
              </div>
              <p className="text-xs font-medium leading-relaxed">{t(`customDoll.${s.key}`)}</p>
            </div>
          ))}
        </div>

        <Card className="rounded-2xl border-0 shadow-md">
          <CardContent className="space-y-6 p-6">
            <div className="space-y-2">
              <Label className="flex items-center gap-2">
                <Camera className="size-4 text-primary" />
                {t("customDoll.uploadImage")}
              </Label>
              <ImageUploader
                value={imageUrl || undefined}
                onUpload={async (url) => {
                  setImageUrl(url);
                  return url;
                }}
                onRemove={async () => setImageUrl("")}
                folder="custom-dolls"
              />
              <p className="text-xs text-muted-foreground">{t("customDoll.uploadHint")}</p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="title">{t("customDoll.title")}</Label>
              <Input
                id="title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder={t("customDoll.titlePlaceholder")}
                maxLength={200}
              />
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="bodyColor">{t("customDoll.bodyColor")}</Label>
                <Input
                  id="bodyColor"
                  value={bodyColor}
                  onChange={(e) => setBodyColor(e.target.value)}
                  placeholder={t("customDoll.bodyColorPlaceholder")}
                  maxLength={100}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="eyeColor">{t("customDoll.eyeColor")}</Label>
                <Input
                  id="eyeColor"
                  value={eyeColor}
                  onChange={(e) => setEyeColor(e.target.value)}
                  placeholder={t("customDoll.eyeColorPlaceholder")}
                  maxLength={100}
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="height">{t("customDoll.height")}</Label>
              <Input
                id="height"
                type="number"
                min={1}
                value={height}
                onChange={(e) => setHeight(e.target.value)}
                placeholder={t("customDoll.heightPlaceholder")}
              />
              <p className="text-xs text-muted-foreground">{t("customDoll.heightHint")}</p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="description">{t("customDoll.description")}</Label>
              <Textarea
                id="description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder={t("customDoll.descriptionPlaceholder")}
                rows={4}
              />
            </div>

            <Button className="w-full rounded-full" size="lg" onClick={submit} disabled={submitting}>
              {submitting ? <Loader2 className="size-4 animate-spin" /> : <Camera className="size-4" />}
              {submitting ? t("customDoll.submitting") : t("customDoll.submit")}
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}